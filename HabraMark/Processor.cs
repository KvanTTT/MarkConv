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
        static Regex ListItemRegex = new Regex($@"^{space}*(\*|-|\+|\d+\.){space}", RegexOptions.Compiled);
        static Regex CodeRegex = new Regex(@"^(~~~|```)", RegexOptions.Compiled);
        static Regex HeaderRegex = new Regex($@"^{space}*#+", RegexOptions.Compiled);
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
            ProcessLinks(lines, headers);

            string result = string.Join("\n", lines);
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
                bool codeSectionMarker = CodeRegex.IsMatch(line);
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
                        prevLine = TrimLine(prevLine);
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
                        bool headerRegex = HeaderRegex.IsMatch(line);
                        bool headerLineRegex = HeaderLineRegex.IsMatch(line);

                        if (Options.LinesMaxLength != 0 &&
                            !string.IsNullOrWhiteSpace(line) && !string.IsNullOrWhiteSpace(prevLine) &&
                            !HeaderRegex.IsMatch(prevLine) && !HeaderLineRegex.IsMatch(prevLine) &&
                            !headerRegex && !headerLineRegex &&
                            !SpecialItemRegex.IsMatch(line) && !ListItemRegex.IsMatch(line))
                        {
                            prevLine = WrapLines(lines, ref prevLineIndex, line.Trim(), prevLine);
                            lineIndex = prevLineIndex + 1;
                            lines.RemoveAt(lineIndex);
                            lineIndex--;
                        }
                        else
                        {
                            if (headerRegex)
                            {
                                int textInd;
                                for (textInd = 0; textInd < line.Length; textInd++)
                                    if (line[textInd] != '#' && line[textInd] != ' ' && line[textInd] != '\t')
                                        break;

                                string header = line.Substring(textInd);
                                if (!string.IsNullOrWhiteSpace(header))
                                {
                                    AddHeader(lines, headers, ref lineIndex, header, line.Remove(textInd).Count(c => c == '#'));
                                }
                            }
                            else if (headerLineRegex)
                            {
                                if (!string.IsNullOrWhiteSpace(prevLine))
                                {
                                    AddHeader(lines, headers, ref lineIndex, prevLine, line.Contains('=') ? 1 : 2);
                                }
                            }
                            if (Options.LinesMaxLength != 0 && lineIndex >= 0 && lineIndex < lines.Count)
                            {
                                lines[lineIndex] = TrimLine(line);
                            }
                        }
                    }
                }
                lineIndex++;
            }

            return headers;
        }

        private static string TrimLine(string line)
        {
            line = ListItemRegex.IsMatch(line) || SpecialItemRegex.IsMatch(line)
                ? line.TrimEnd() : line.Trim();
            return line;
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

        private void ProcessLinks(List<string> lines, List<Header> headers)
        {
            int lineIndex = 0;
            int imageLinkNumber = 0;
            bool codeSection = false;
            while (lineIndex < lines.Count)
            {
                string line = lines[lineIndex];
                if (CodeRegex.IsMatch(line))
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
                    int linkIndex = 0;
                    Link link;
                    while ((link = Link.ParseNextLink(line, linkIndex)) != null)
                    {
                        Header header;
                        int linkLength;
                        if (Options.OutputRelativeLinksKind != RelativeLinksKind.Default && link.IsRelative && !link.IsImage)
                        {
                            string inputAddress = Header.GetAppropriateLink(Options.InputRelativeLinksKind, link.Address);
                            string outputAddress =
                                (header = headers.FirstOrDefault(h => h.GetAppropriateLink(Options.InputRelativeLinksKind) == inputAddress)) != null
                                ? header.GetAppropriateLink(Options.OutputRelativeLinksKind)
                                : Header.GetAppropriateLink(Options.OutputRelativeLinksKind, inputAddress);
                            Link newLink = new Link(link.Title, outputAddress) { IsRelative = true };
                            line = Replace(line, link, newLink, out linkLength);
                        }
                        else if (link.IsImage)
                        {
                            if (imageLinkNumber == 0 && !string.IsNullOrWhiteSpace(Options.HeaderImageLink))
                            {
                                Link newLink = new Link(link.ToString(), Options.HeaderImageLink);
                                line = Replace(line, link, newLink, out linkLength);
                            }
                            else
                            {
                                linkLength = link.ToString().Length;
                            }
                            imageLinkNumber++;
                        }
                        else
                        {
                            linkLength = link.ToString().Length;
                        }
                        linkIndex += linkLength;
                    }
                    lines[lineIndex] = line;
                }
                lineIndex++;
            }
        }

        private string Replace(string line, Link oldLink, Link newLink, out int newLinkLength)
        {
            string newLinkString = newLink.ToString();
            newLinkLength = newLinkString.Length;
            return line.Remove(oldLink.Index, oldLink.ToString().Length).Insert(oldLink.Index, newLinkString);
        }
    }
}
