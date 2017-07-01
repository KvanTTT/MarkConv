﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static HabraMark.MarkdownRegex;

namespace HabraMark
{
    public class LinesProcessor
    {
        public ILogger Logger { get; set; }

        public ProcessorOptions Options { get; set; }

        public LinesProcessor(ProcessorOptions options) => Options = options ?? new ProcessorOptions();

        public LinesProcessorResult Process(IList<string> lines)
        {
            bool codeSection = false;
            var resultLines = new List<string>();
            var headers = new List<Header>();
            
            for (int lineIndex = 0; lineIndex <= lines.Count; lineIndex++)
            {
                string line = lineIndex < lines.Count ? lines[lineIndex] : string.Empty;
                bool codeSectionMarker = CodeSectionRegex.IsMatch(line);

                if (codeSectionMarker)
                {
                    codeSection = !codeSection;
                }

                if (codeSection)
                {
                    resultLines.Add(line);
                }
                else
                {
                    string lastResultLine = resultLines.Count > 0 ? resultLines[resultLines.Count - 1] : "";

                    if (Options.LinesMaxLength > 0 && lastResultLine.Length > Options.LinesMaxLength &&
                        !string.IsNullOrWhiteSpace(lastResultLine) &&
                        !HeaderRegex.IsMatch(lastResultLine) && !HeaderLineRegex.IsMatch(lastResultLine))
                    {
                        WrapLines(resultLines, lines, lastResultLine);
                        lastResultLine = resultLines[resultLines.Count - 1];
                    }

                    if (Options.RemoveUnwantedBreaks &&
                        string.IsNullOrWhiteSpace(lastResultLine) && string.IsNullOrWhiteSpace(line))
                    {
                        if (resultLines.Count > 0 && lineIndex == lines.Count)
                        {
                            resultLines.RemoveAt(resultLines.Count - 1);
                        }
                    }
                    else
                    {
                        Match headerMatch = HeaderRegex.Match(line);
                        Match listItemMatch = ListItemRegex.Match(line);
                        bool isHeaderLineMatch = HeaderLineRegex.IsMatch(line);
                        bool isSpecialItemMatch = SpecialItemRegex.IsMatch(line);

                        if (Options.LinesMaxLength != 0 &&
                            !string.IsNullOrWhiteSpace(lastResultLine) &&
                            !HeaderRegex.IsMatch(lastResultLine) && !HeaderLineRegex.IsMatch(lastResultLine) &&
                            !CodeSectionRegex.IsMatch(lastResultLine) &&

                            !string.IsNullOrWhiteSpace(line) &&
                            !headerMatch.Success && !isHeaderLineMatch &&
                            !CodeSectionRegex.IsMatch(line) &&
                            !SpecialItemRegex.IsMatch(line) && !listItemMatch.Success)
                        {
                            WrapLines(resultLines, lines, line.Trim(), lastResultLine);
                        }
                        else
                        {
                            string resultLine = line;
                            if (headerMatch.Success)
                            {
                                string headerChars = headerMatch.Groups[1].Value;
                                string header = headerMatch.Groups[2].Value;
                                if (Options.Normalize)
                                {
                                    header = header.TrimEnd('#', ' ', '\t');
                                    resultLine = $"{headerChars} {header}";
                                }
                                else if (Options.LinesMaxLength != 0)
                                {
                                    resultLine = line.Trim();
                                }

                                int level = headerChars.Length;
                                if (!AddHeader(headers, header, level))
                                {
                                    resultLine = null;
                                }
                            }
                            else if (isHeaderLineMatch)
                            {
                                int level = line.Contains("=") ? 1 : 2;
                                if (!AddHeader(headers, lastResultLine, level))
                                {
                                    resultLine = null;
                                }

                                if (Options.Normalize)
                                {
                                    if (resultLines.Count >= 0)
                                    {
                                        resultLines.RemoveAt(resultLines.Count - 1);
                                    }
                                    resultLine = $"{new string('#', level)} {lastResultLine}";
                                }
                                else if (Options.LinesMaxLength != 0)
                                {
                                    resultLine = line.Trim();
                                }
                            }
                            else if (listItemMatch.Success)
                            {
                                if (Options.Normalize)
                                {
                                    string itemChar = listItemMatch.Groups[1].Value;
                                    if (!char.IsDigit(itemChar[0]))
                                        itemChar = "*";
                                    resultLine = $"{itemChar} {listItemMatch.Groups[2].Value}";
                                }
                                else if (Options.LinesMaxLength != 0)
                                {
                                    resultLine = line.TrimEnd();
                                }
                            }
                            else if (Options.LinesMaxLength != 0 && lineIndex >= 0 && lineIndex < lines.Count)
                            {
                                resultLine = isSpecialItemMatch ? line.TrimEnd() : line.Trim();
                            }
                            if (resultLine != null && lineIndex >= 0 && lineIndex < lines.Count)
                            {
                                resultLines.Add(resultLine);
                            }
                        }
                    }
                }
            }

            return new LinesProcessorResult(resultLines, headers);
        }

        private void WrapLines(List<string> resultLines, IList<string> lines, string str, string initStr = "")
        {
            string[] words = SplitForSoftWrap(str);
            int linesMaxLength = Options.LinesMaxLength == -1 ? int.MaxValue : Options.LinesMaxLength;
            var buffer = new StringBuilder(Options.LinesMaxLength == -1 ? str.Length : Options.LinesMaxLength);
            if (initStr != "")
            {
                buffer.Append(initStr);
                buffer.Append(' ');
            }
            resultLines.RemoveAt(resultLines.Count - 1);
            for (int i = 0; i < words.Length; i++)
            {
                if (buffer.Length + words[i].Length <= linesMaxLength)
                {
                    buffer.Append(words[i]);
                    buffer.Append(' ');
                }
                else
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Remove(buffer.Length - 1, 1);
                        resultLines.Add(buffer.ToString());
                        buffer.Clear();
                    }
                    buffer.Append(words[i]);
                    buffer.Append(' ');
                }
            }
            if (buffer.Length > 0)
            {
                buffer.Remove(buffer.Length - 1, 1);
                resultLines.Add(buffer.ToString());
            }
        }

        private string[] SplitForSoftWrap(string str)
        {
            string[] splitted = str.Split(SpaceChars, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            foreach (string s in splitted)
            {
                if (SpecialCharsRegex.IsMatch(s) && result.Count > 0)
                {
                    result[result.Count - 1] = $"{result[result.Count - 1]} {s}";
                }
                else
                {
                    result.Add(s);
                }
            }
            return result.ToArray();
        }

        private bool AddHeader(List<Header> headers, string header, int level)
        {
            if (Options.RemoveTitleHeader && level == 1 && headers.Count == 0)
            {
                return false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(header))
                {
                    headers.Add(new Header(header, level, headers));
                }
                return true;
            }
        }
    }
}
