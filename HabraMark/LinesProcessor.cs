using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static HabraMark.MarkdownRegex;

namespace HabraMark
{
    public class LinesProcessor
    {
        public ILogger Logger { get; set; }

        public ProcessorOptions Options { get; set; }

        public LinesProcessor(ProcessorOptions options = null) => Options = options ?? new ProcessorOptions();

        public LinesProcessorResult Process(string text)
        {
            return Process(text.Split(LineBreaks, StringSplitOptions.None));
        }

        public LinesProcessorResult Process(IList<string> lines)
        {
            bool codeSection = false;
            var resultLines = new List<string>(lines.Count);
            var headers = new List<Header>();
            
            for (int lineIndex = 0; lineIndex <= lines.Count; lineIndex++)
            {
                string line = lineIndex < lines.Count ? lines[lineIndex] : string.Empty;
                Match codeSectionMarkerMatch = CodeSectionRegex.Match(line);

                if (codeSectionMarkerMatch.Success)
                {
                    codeSection = !codeSection;
                }

                if ((codeSection && line.IndexOf("```", codeSectionMarkerMatch.Length) == -1) ||
                    (!codeSection && codeSectionMarkerMatch.Success))
                {
                    resultLines.Add(line);
                }
                else
                {
                    if (codeSection)
                    {
                        codeSection = false;
                    }

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
                                if (!AddHeader(headers, header, level, lineIndex, resultLines.Count - 1))
                                {
                                    resultLine = null;
                                }
                            }
                            else if (isHeaderLineMatch)
                            {
                                int level = line.Contains("=") ? 1 : 2;
                                if (!AddHeader(headers, lastResultLine, level, lineIndex, resultLines.Count - 2))
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

        public List<string> GenerateTableOfContents(LinesProcessorResult linesProcessorResult)
        {
            List<Header> headers = linesProcessorResult.Headers;
            var tableOfContents = new List<string>(headers.Count);

            if (headers.Count == 0)
                return tableOfContents;

            int firstLevel = headers[0].Level;
            foreach (Header header in headers)
            {
                string indent = Repeat(Options.IndentString, header.Level - firstLevel);
                Link link = new Link(header.Title, header.Links[Options.OutputMarkdownType].FullLink, isRelative: true);
                tableOfContents.Add($"{indent}* {link}");
            }
            return tableOfContents;
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
            var result = new List<string>(str.Length / 2);

            int lastNotWsIndex = 0;
            bool codeSection = false;
            char[] chars = str.ToCharArray();
            int i = 0;
            while (i <= chars.Length)
            {
                if (i + 2 < chars.Length &&
                    chars[i] == '`' && chars[i + 1] == '`' && chars[i + 2] == '`')
                {
                    codeSection = !codeSection;
                    i += 3;
                }
                else if (!codeSection)
                {
                    if (i == chars.Length || chars[i] == ' ' || chars[i] == '\t')
                    {
                        int wordLength = i - lastNotWsIndex;
                        if (wordLength != 0)
                        {
                            string word = new string(chars, lastNotWsIndex, wordLength);
                            if (SpecialCharsRegex.IsMatch(word) && result.Count > 0)
                            {
                                result[result.Count - 1] = $"{result[result.Count - 1]} {word}";
                            }
                            else
                            {
                                result.Add(word);
                            }
                        }
                    }
                    else if (i - 1 >= 0 && (chars[i - 1] == ' ' || chars[i - 1] == '\t'))
                    {
                        lastNotWsIndex = i;
                    }
                    i++;
                }
                else
                {
                    i++;
                }
            }

            return result.ToArray();
        }

        private bool AddHeader(List<Header> headers, string header, int level, int sourceLineIndex, int destLineIndex)
        {
            if (Options.RemoveTitleHeader && level == 1 && headers.Count == 0)
            {
                return false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(header))
                {
                    if (headers.Any() && level < headers.Min(h => h.Level))
                    {
                        Logger?.Warn($"Header \"{header}\" level {level} at line {sourceLineIndex + 1} is incorrect");
                    }
                    headers.Add(new Header(header, level, headers)
                    {
                        SourceLineIndex = sourceLineIndex,
                        DestLineIndex = destLineIndex >= 0 ? destLineIndex : 0
                    });
                }
                return true;
            }
        }

        private static string Repeat(string value, int count)
        {
            if (count <= 0)
                return "";

            return new StringBuilder(value.Length * count).Insert(0, value, count).ToString();
        }
    }
}
