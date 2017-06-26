using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HabraMark
{
    public class Processor
    {
        static string[] lineBreaks = new string[] { "\n", "\r\n" };
        static string space = @"[ \t]";
        static Regex NotTextRegex = new Regex($@"^{space}*(>|\*{space}|-{space}|\d\.{space}|\|)", RegexOptions.Compiled);
        static Regex CodeRegex = new Regex(@"^(~~~|```)", RegexOptions.Compiled);
        static Regex HeaderRegex = new Regex($@"^{space}*#+", RegexOptions.Compiled);
        static Regex HeaderLineRegex = new Regex($@"^{space}*(-+|=+){space}*$", RegexOptions.Compiled);
        static Regex DetailsOpenTagRegex = new Regex($@"<\s*details\s*>");
        static Regex DetailsCloseTagRegex = new Regex($@"<?\s*details\s*>");

        /// <summary>
        /// 0 - not change
        /// -1 - concat lines
        /// </summary>
        public int LinesMaxLength { get; set; } = -1;

        public bool RemoveTitleHeader { get; set; } = true;

        public bool ReplaceRelativeLinks { get; set; } = true;

        public string HeaderImageLink { get; set; } = string.Empty;

        public bool ReplaceSpoilers { get; set; } = true;

        public bool Trim { get; set; } = true;

        public string Process(string original)
        {
            List<string> lines = original.Split(lineBreaks, StringSplitOptions.None).ToList();

            List<Header> headers = ProcessLinesAndCollectHeaders(lines);
            ProcessLinks(lines, headers);

            if (Trim)
            {
                lines = TrimLines(lines);
            }

            string result = string.Join("\n", lines);
            return result;
        }

        private List<Header> ProcessLinesAndCollectHeaders(List<string> lines)
        {
            int lineIndex = 0;
            bool codeSection = false;
            List<Header> headers = new List<Header>();
            while (lineIndex < lines.Count)
            {
                string line = lines[lineIndex];
                string prevLine = lineIndex >= 1 ? lines[lineIndex - 1] : string.Empty;
                bool codeSectionMarker = CodeRegex.IsMatch(line);
                bool headerRegex = HeaderRegex.IsMatch(line);
                bool headerLineRegex = HeaderLineRegex.IsMatch(line);

                if (LinesMaxLength == -1 &&
                    !string.IsNullOrWhiteSpace(line) && !string.IsNullOrWhiteSpace(prevLine) &&
                    !NotTextRegex.IsMatch(line) &&
                    !headerRegex && !headerLineRegex && !codeSection)
                {
                    lines[lineIndex - 1] = $"{prevLine.TrimEnd()} {line.TrimStart()}";
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

                    if (headerLineRegex)
                    {
                        if (!string.IsNullOrWhiteSpace(prevLine))
                        {
                            AddHeader(lines, headers, ref lineIndex, prevLine, line.Contains('=') ? 1 : 2);
                        }
                    }

                    if (codeSectionMarker)
                    {
                        codeSection = !codeSection;
                    }
                }
                lineIndex++;
            }
            return headers;
        }

        private void AddHeader(List<string> lines, List<Header> headers, ref int lineIndex, string header, int level)
        {
            if (RemoveTitleHeader && level == 1 && headers.Count == 0)
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
                }

                if (!codeSection)
                {
                    int linkIndex = 0;
                    Link link;
                    while ((link = Link.ParseNextLink(line, linkIndex)) != null)
                    {
                        string lowerAddress = link.Address.ToLowerInvariant();
                        Header header;
                        int linkLength;
                        if (ReplaceRelativeLinks && link.IsRelative && !link.IsImage &&
                            (header = headers.FirstOrDefault(h => h.FullLink == lowerAddress)) != null)
                        {
                            Link newLink = new Link(link.Title, header.FullHabraLink) { IsRelative = true };
                            line = Replace(line, link, newLink, out linkLength);
                        }
                        else if (link.IsImage)
                        {
                            if (imageLinkNumber == 0 && !string.IsNullOrWhiteSpace(HeaderImageLink))
                            {
                                Link newLink = new Link(link.ToString(), HeaderImageLink);
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

        private static List<string> TrimLines(List<string> lines)
        {
            int firstInd = 0;
            while (firstInd < lines.Count && string.IsNullOrWhiteSpace(lines[firstInd]))
                firstInd++;
            int lastInd = lines.Count - 1;
            while (lastInd >= 0 && string.IsNullOrWhiteSpace(lines[lastInd]))
                lastInd--;

            if (firstInd != 0 || lastInd != lines.Count - 1)
                lines = lines.Skip(firstInd).Take(lastInd - firstInd + 1).ToList();
            return lines;
        }
    }
}
