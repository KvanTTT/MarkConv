using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static MarkConv.MarkdownRegex;

namespace MarkConv
{
    public class LinesProcessor
    {
        private readonly List<string> _indents = new List<string> { "" };

        public ILogger Logger { get; set; }

        public ProcessorOptions Options { get; }

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
            int processedHeadersCount = 0;

            for (int lineIndex = 0; lineIndex <= lines.Count; lineIndex++)
            {
                string line = lineIndex < lines.Count ? lines[lineIndex] : string.Empty;
                Match codeSectionMarkerMatch = CodeSectionCloseRegex.Match(line);

                if (codeSectionMarkerMatch.Success)
                {
                    codeSection = !codeSection;
                }

                if (codeSection && line.IndexOf("```", codeSectionMarkerMatch.Length, StringComparison.Ordinal) == -1 ||
                    !codeSection && codeSectionMarkerMatch.Success)
                {
                    resultLines.Add(line);
                }
                else
                {
                    if (codeSection)
                    {
                        if (Options.Normalize)
                        {
                            line = line.Replace("```", "`");
                        }
                        codeSection = false;
                    }

                    string lastResultLine = resultLines.Count > 0 ? resultLines[^1] : "";

                    if (Options.LinesMaxLength > 0 && lastResultLine.Length > Options.LinesMaxLength &&
                        !string.IsNullOrWhiteSpace(lastResultLine) &&
                        !HeaderRegex.IsMatch(lastResultLine) && !HeaderLineRegex.IsMatch(lastResultLine))
                    {
                        WrapLines(resultLines, lastResultLine);
                        lastResultLine = resultLines[^1];
                    }

                    if (Options.NormalizeBreaks &&
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

                        bool isLastResultLineHeaderOrCodeSection =
                            HeaderRegex.IsMatch(lastResultLine) ||
                            HeaderLineRegex.IsMatch(lastResultLine) ||
                            CodeSectionCloseRegex.IsMatch(lastResultLine);

                        bool isLastLineHeader = headerMatch.Success;
                        bool isLastLineSpecial = isLastLineHeader || isHeaderLineMatch ||
                            SpecialItemRegex.IsMatch(line) || listItemMatch.Success;

                        bool isLastResultLineWhiteSpace = string.IsNullOrWhiteSpace(lastResultLine);

                        bool isLastLineWhiteSpace = string.IsNullOrWhiteSpace(line);

                        if (Options.LinesMaxLength != 0 &&
                            !isLastResultLineHeaderOrCodeSection &&
                            !isLastResultLineWhiteSpace &&
                            !isLastLineSpecial &&
                            !isLastLineWhiteSpace)
                        {
                            WrapLines(resultLines, line.Trim(), lastResultLine);
                        }
                        else
                        {
                            if (Options.NormalizeBreaks)
                            {
                                if (isLastResultLineHeaderOrCodeSection && !isLastLineWhiteSpace ||
                                    isLastLineHeader && !isLastResultLineWhiteSpace)
                                {
                                    resultLines.Add("");
                                }
                            }

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
                                if (!AddHeader(headers, processedHeadersCount, header, level, lineIndex, resultLines.Count - 1))
                                {
                                    resultLine = null;
                                }
                                processedHeadersCount++;
                            }
                            else if (isHeaderLineMatch)
                            {
                                int level = line.Contains("=") ? 1 : 2;
                                if (!AddHeader(headers, processedHeadersCount, lastResultLine, level, lineIndex, resultLines.Count - 2))
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
                                processedHeadersCount++;
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
                Link link = new Link(header.Title, header.Links[Options.OutputMarkdownType].FullLink, linkType: LinkType.Relative);
                tableOfContents.Add($"{indent}* {link}");
            }
            return tableOfContents;
        }

        private void WrapLines(List<string> resultLines, string str, string initStr = "")
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
            int charInd = 0;
            while (charInd <= chars.Length)
            {
                if (charInd + 2 < chars.Length &&
                    chars[charInd] == '`' && chars[charInd + 1] == '`' && chars[charInd + 2] == '`')
                {
                    codeSection = !codeSection;
                    charInd += 3;
                }
                else if (!codeSection)
                {
                    if (charInd == chars.Length || chars[charInd] == ' ' || chars[charInd] == '\t')
                    {
                        int wordLength = charInd - lastNotWsIndex;
                        if (wordLength != 0)
                        {
                            string word = new string(chars, lastNotWsIndex, wordLength);
                            if (SpecialCharsRegex.IsMatch(word) && result.Count > 0)
                            {
                                result[^1] = $"{result[^1]} {word}";
                            }
                            else
                            {
                                result.Add(word);
                            }
                        }
                    }
                    else if (charInd - 1 >= 0 && (chars[charInd - 1] == ' ' || chars[charInd - 1] == '\t'))
                    {
                        lastNotWsIndex = charInd;
                    }
                    charInd++;
                }
                else
                {
                    charInd++;
                }
            }

            return result.ToArray();
        }

        private bool AddHeader(List<Header> headers, int processedHeadersCount, string header, int level, int sourceLineIndex, int destLineIndex)
        {
            if (Options.RemoveTitleHeader && level == 1 && processedHeadersCount == 0)
            {
                return false;
            }

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

        private string Repeat(string value, int count)
        {
            if (count < 0)
            {
                return "";
            }

            if (_indents.Count <= count)
            {
                var stringBuilder = new StringBuilder(value.Length * count);
                stringBuilder.Append(_indents[^1]);

                for (int i = _indents.Count; i <= count; i++)
                {
                    stringBuilder.Append(value);
                    _indents.Add(stringBuilder.ToString());
                }
            }

            return _indents[count];
        }
    }
}
