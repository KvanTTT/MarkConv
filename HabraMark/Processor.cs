using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HabraMark
{
    public class Processor
    {
        static string[] lineBreaks = new string[] { "\n", "\r\n" };
        static string space = @"[ \t]";
        static char[] spaceChars = new char[] { ' ', '\t' };
        static Regex SpecialCharsRegex = new Regex($@"^(>|\*|-|\+|\d+\.|\||=)$", RegexOptions.Compiled);
        static Regex SpecialItemRegex = new Regex($@"^{space}*(>|\|)", RegexOptions.Compiled);
        static Regex ListItemRegex = new Regex($@"^{space}*(\*|-|\+|\d+\.){space}(.+)", RegexOptions.Compiled);
        static Regex CodeSectionRegex = new Regex($@"^{space}*(~~~|```)", RegexOptions.Compiled | RegexOptions.Multiline);
        static Regex HeaderRegex = new Regex($@"^{space}*(#+){space}*(.+)", RegexOptions.Compiled);
        static Regex HeaderLineRegex = new Regex($@"^{space}*(-+|=+){space}*$", RegexOptions.Compiled);
        static Regex DetailsOpenTagRegex = new Regex($@"<\s*details\s*>");
        static Regex DetailsCloseTagRegex = new Regex($@"<\s*/details\s*>");

        public ProcessorOptions Options { get; set; }

        public Processor(ProcessorOptions options = null)
        {
            Options = options ?? new ProcessorOptions();
        }

        public string Process(string original)
        {
            List<string> lines = original.Split(lineBreaks, StringSplitOptions.None).ToList();

            List<Header> headers = ProcessLinesAndCollectHeaders(lines);
            string result = string.Join("\n", lines);
            result = ProcessLinksAndHtmlElements(result, headers);
            return result;
        }

        private List<Header> ProcessLinesAndCollectHeaders(List<string> lines)
        {
            int lineIndex = 0;
            bool codeSection = false;
            List<Header> headers = new List<Header>();
            while (lineIndex <= lines.Count)
            {
                string line = lineIndex < lines.Count ? lines[lineIndex] : string.Empty;
                bool codeSectionMarker = CodeSectionRegex.IsMatch(line);
                if (codeSectionMarker)
                {
                    codeSection = !codeSection;
                    if (!codeSection)
                    {
                        lineIndex++;
                        continue;
                    }
                }

                if (!codeSection)
                {
                    int prevLineIndex = lineIndex - 1;
                    string prevLine = lineIndex >= 1 ? lines[prevLineIndex] : string.Empty;

                    if (Options.LinesMaxLength > 0 && prevLine.Length > Options.LinesMaxLength &&
                        !string.IsNullOrWhiteSpace(prevLine) &&
                        !HeaderRegex.IsMatch(prevLine) && !HeaderLineRegex.IsMatch(prevLine))
                    {
                        prevLine = WrapLines(lines, ref prevLineIndex, prevLine);
                        lineIndex = prevLineIndex + 1;
                    }

                    if (Options.RemoveUnwantedBreaks &&
                        string.IsNullOrWhiteSpace(prevLine) && string.IsNullOrWhiteSpace(line))
                    {
                        lines.RemoveAt(lineIndex < lines.Count ? lineIndex : prevLineIndex);
                        lineIndex--;
                    }
                    else
                    {
                        Match headerMatch = HeaderRegex.Match(line);
                        Match listItemMatch = ListItemRegex.Match(line);
                        bool isHeaderLineMatch = HeaderLineRegex.IsMatch(line);
                        bool isSpecialItemMatch = SpecialItemRegex.IsMatch(line);

                        if (Options.LinesMaxLength != 0 &&
                            !string.IsNullOrWhiteSpace(prevLine) &&
                            !HeaderRegex.IsMatch(prevLine) && !HeaderLineRegex.IsMatch(prevLine) &&
                            !CodeSectionRegex.IsMatch(prevLine) &&

                            !string.IsNullOrWhiteSpace(line)
                            && !headerMatch.Success && !isHeaderLineMatch &&
                            !SpecialItemRegex.IsMatch(line) && !listItemMatch.Success)
                        {
                            prevLine = WrapLines(lines, ref prevLineIndex, line.Trim(), prevLine);
                            lineIndex = prevLineIndex + 1;
                            lines.RemoveAt(lineIndex);
                            lineIndex--;
                        }
                        else
                        {
                            if (headerMatch.Success)
                            {
                                string headerChars = headerMatch.Groups[1].Value;
                                string header = headerMatch.Groups[2].Value;
                                if (Options.Normalize)
                                {
                                    header = header.TrimEnd('#', ' ', '\t');
                                    lines[lineIndex] = $"{headerChars} {header}";
                                }
                                else if (Options.LinesMaxLength != 0)
                                {
                                    lines[lineIndex] = line.Trim();
                                }

                                if (!string.IsNullOrWhiteSpace(header))
                                {
                                    int level = headerChars.Length;
                                    AddHeader(lines, headers, ref lineIndex, header, level);
                                }
                            }
                            else if (isHeaderLineMatch)
                            {
                                int level = line.Contains('=') ? 1 : 2;
                                if (!string.IsNullOrWhiteSpace(prevLine))
                                {
                                    AddHeader(lines, headers, ref lineIndex, prevLine, level);
                                }
                                if (Options.Normalize)
                                {
                                    lines[lineIndex] = $"{new string('#', level)} {prevLine}";
                                    if (prevLineIndex >= 0)
                                    {
                                        lines.RemoveAt(prevLineIndex);
                                        lineIndex--;
                                    }
                                }
                                else if (Options.LinesMaxLength != 0)
                                {
                                    lines[lineIndex] = line.Trim();
                                }
                            }
                            else if (listItemMatch.Success)
                            {
                                if (Options.Normalize)
                                {
                                    string itemChar = listItemMatch.Groups[1].Value;
                                    if (!char.IsDigit(itemChar[0]))
                                        itemChar = "*";
                                    lines[lineIndex] = $"{itemChar} {listItemMatch.Groups[2].Value}";
                                }
                                else if (Options.LinesMaxLength != 0)
                                {
                                    lines[lineIndex] = line.TrimEnd();
                                }
                            }
                            else if (Options.LinesMaxLength != 0 && lineIndex >= 0 && lineIndex < lines.Count)
                            {
                                lines[lineIndex] = isSpecialItemMatch ? line.TrimEnd() : line.Trim();
                            }
                        }
                    }
                }
                lineIndex++;
            }

            return headers;
        }

        private string WrapLines(List<string> lines, ref int lineIndex, string str, string initStr = "")
        {
            string lastLine = "";
            string[] words = SplitForSoftWrap(str);
            int linesMaxLength = Options.LinesMaxLength == -1 ? int.MaxValue : Options.LinesMaxLength;
            var buffer = new StringBuilder(Options.LinesMaxLength == -1 ? str.Length : Options.LinesMaxLength);
            if (initStr != "")
            {
                buffer.Append(initStr);
                buffer.Append(' ');
            }
            lines.RemoveAt(lineIndex);
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
                        lastLine = buffer.ToString();
                        lines.Insert(lineIndex++, lastLine);
                        buffer.Clear();
                    }
                    buffer.Append(words[i]);
                    buffer.Append(' ');
                }
            }
            if (buffer.Length > 0)
            {
                buffer.Remove(buffer.Length - 1, 1);
                lastLine = buffer.ToString();
                lines.Insert(lineIndex++, lastLine);
            }
            lineIndex--;
            return lastLine;
        }

        private string[] SplitForSoftWrap(string str)
        {
            string[] splitted = str.Split(spaceChars, StringSplitOptions.RemoveEmptyEntries);
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

        private void AddHeader(List<string> lines, List<Header> headers, ref int lineIndex, string header, int level)
        {
            if (Options.RemoveTitleHeader && level == 1 && headers.Count == 0)
            {
                lines.RemoveAt(lineIndex);
                lineIndex--;
            }
            else
            {
                Header.AddHeader(headers, header, level);
            }
        }

        private string ProcessLinksAndHtmlElements(string text, List<Header> headers)
        {
            int imageLinkNumber = 0;
            var result = new StringBuilder(text.Length);

            int index = 0;
            while (index < text.Length)
            {
                Match startCodeFragmentMatch = CodeSectionRegex.Match(text, index);
                int startCodeFragmentIndex = startCodeFragmentMatch.Success ? startCodeFragmentMatch.Index : text.Length;

                Link link;
                while ((link = Link.ParseNextLink(text, index, startCodeFragmentIndex - index)) != null)
                {
                    result.Append(text.Substring(index, link.Index - index));

                    string linkString;
                    if (Options.OutputRelativeLinksKind != RelativeLinksKind.Default && link.IsRelative && !link.IsImage)
                    {
                        Header header;
                        string inputAddress = Header.GetAppropriateLink(Options.InputRelativeLinksKind, link.Address);
                        string outputAddress =
                            (header = headers.FirstOrDefault(h => h.GetAppropriateLink(Options.InputRelativeLinksKind) == inputAddress)) != null
                            ? header.GetAppropriateLink(Options.OutputRelativeLinksKind)
                            : Header.GetAppropriateLink(Options.OutputRelativeLinksKind, inputAddress);

                        Link newLink = new Link(link.Title, outputAddress) { IsRelative = true };
                        linkString = newLink.ToString();
                    }
                    else if (link.IsImage)
                    {
                        if (imageLinkNumber == 0 && !string.IsNullOrWhiteSpace(Options.HeaderImageLink))
                        {
                            Link newLink = new Link(link.ToString(), Options.HeaderImageLink);
                            linkString = newLink.ToString();
                        }
                        else
                        {
                            linkString = link.ToString();
                        }
                        imageLinkNumber++;
                    }
                    else
                    {
                        linkString = link.ToString();
                    }

                    result.Append(linkString);
                    index = link.Index + link.Length;
                }

                result.Append(text.Substring(index, startCodeFragmentIndex - index));

                if (startCodeFragmentMatch.Success)
                {
                    Match endCodeFragmentMatch = CodeSectionRegex.Match(text, startCodeFragmentMatch.Index + startCodeFragmentMatch.Length);
                    index = endCodeFragmentMatch.Success
                        ? endCodeFragmentMatch.Index + endCodeFragmentMatch.Length
                        : text.Length;

                    result.Append(text.Substring(startCodeFragmentMatch.Index, index - startCodeFragmentMatch.Index));
                }
                else
                {
                    index = text.Length;
                }
            }

            return result.ToString();
        }

        private string Replace(string line, Link oldLink, Link newLink, out int newLinkLength)
        {
            string newLinkString = newLink.ToString();
            newLinkLength = newLinkString.Length;
            return line.Remove(oldLink.Index, oldLink.ToString().Length).Insert(oldLink.Index, newLinkString);
        }
    }
}
