using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static HabraMark.MarkdownRegex;

namespace HabraMark
{
    public class LinksHtmlProcessor
    {
        private int _imageLinkNumber;
        private List<Header> _headers;

        public ProcessorOptions Options { get; set; }

        public ILogger Logger { get; set; }

        public LinksHtmlProcessor(ProcessorOptions options) => Options = options ?? new ProcessorOptions();

        public string Process(string text, List<Header> headers)
        {
            _imageLinkNumber = 0;
            _headers = headers;
            var result = new StringBuilder(text.Length);

            int index = 0;
            while (index < text.Length)
            {
                Match startCodeFragmentMatch = CodeSectionRegex.Match(text, index);
                int startCodeFragmentIndex = startCodeFragmentMatch.Success ? startCodeFragmentMatch.Index : text.Length;

                Dictionary<ElementType, Match> matches = new Dictionary<ElementType, Match>();
                Tuple<ElementType, Match> matchResult = null;

                while ((matchResult = NextMatch(text, index, startCodeFragmentIndex - index, matchResult, matches)) != null)
                {
                    ElementType elementType = matchResult.Item1;
                    Match match = matchResult.Item2;
                    result.Append(text.Substring(index, match.Index - index));

                    string processedMatch
                        = elementType == ElementType.Link ? ProcessLink(_headers, ref _imageLinkNumber, match)
                        : elementType == ElementType.DetailsElement ? ConvertDetailsElement(match)
                        : elementType == ElementType.SummaryElements ? ConvertSummaryElements(match)
                        : elementType == ElementType.SpoilerOpenElement ? ConvertSpoilerOpenElement(match)
                        : elementType == ElementType.SpoilerCloseElement ? ConvertSpolierCloseElement(match)
                        : "";

                    result.Append(processedMatch);
                    index = match.Index + match.Length;

                    if (string.IsNullOrWhiteSpace(processedMatch))
                    {
                        while (index < text.Length && char.IsWhiteSpace(text[index]))
                            index++;
                        while (result.Length > 0 && (SpaceChars.Contains(result[result.Length - 1])))
                            result.Remove(result.Length - 1, 1);
                    }
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

        private Tuple<ElementType, Match> NextMatch(string text, int index, int length,
            Tuple<ElementType, Match> prevMatch, Dictionary<ElementType, Match> prevMatches)
        {
            if (prevMatch == null)
            {
                prevMatches[ElementType.Link] = GetMatch(text, index, length, ElementType.Link);
                if (Options.InputMarkdownType != MarkdownType.Habrahabr &&
                    Options.OutputMarkdownType == MarkdownType.Habrahabr)
                {
                    prevMatches[ElementType.DetailsElement] = GetMatch(text, index, length, ElementType.DetailsElement);
                    prevMatches[ElementType.SummaryElements] = GetMatch(text, index, length, ElementType.SummaryElements);
                }
                if ((Options.InputMarkdownType != MarkdownType.GitHub &&
                     Options.InputMarkdownType != MarkdownType.VisualCode) &&
                    (Options.OutputMarkdownType == MarkdownType.GitHub ||
                     Options.OutputMarkdownType == MarkdownType.VisualCode))
                {
                    prevMatches[ElementType.SpoilerOpenElement] = GetMatch(text, index, length, ElementType.SpoilerOpenElement);
                    prevMatches[ElementType.SpoilerCloseElement] = GetMatch(text, index, length, ElementType.SpoilerCloseElement);
                }
            }
            else
            {
                prevMatches[prevMatch.Item1] = GetMatch(text, index, length, prevMatch.Item1);
            }

            Tuple<ElementType, Match> result = null;
            foreach (KeyValuePair<ElementType, Match> match in prevMatches)
            {
                if (match.Value.Success && match.Value.Index < (result?.Item2.Index ?? int.MaxValue))
                {
                    result = new Tuple<ElementType, Match>(match.Key, match.Value);
                }
            }

            return result;
        }

        private Match GetMatch(string text, int index, int length, ElementType elementType)
        {
            Regex regex
                = elementType == ElementType.Link ? LinkRegex
                : elementType == ElementType.DetailsElement ? DetailsTagRegex
                : elementType == ElementType.SummaryElements ? SummaryTagsRegex
                : elementType == ElementType.SpoilerOpenElement ? SpoilerOpenTagRegex
                : elementType == ElementType.SpoilerCloseElement ? SpoilerCloseTagRegex
                : throw new NotImplementedException($"Regex for {elementType} has not been found");

            return regex.Match(text, index, length);
        }

        private string ProcessLink(List<Header> headers, ref int imageLinkNumber, Match match)
        {
            bool isImage = !string.IsNullOrEmpty(match.Groups[1].Value);
            string title = match.Groups[2].Value;
            bool isRelative = !string.IsNullOrEmpty(match.Groups[4].Value);
            string address = match.Groups[5].Value;

            string linkString;
            if (Options.OutputMarkdownType != MarkdownType.Default && isRelative && !isImage)
            {
                string inputAddress = Header.GenerateLink(Options.InputMarkdownType, address);
                Header header = headers.FirstOrDefault(h => h.Links[Options.InputMarkdownType].FullLink == inputAddress);
                string outputAddress;
                if (header != null)
                {
                    outputAddress = header.Links[Options.OutputMarkdownType].FullLink;
                }
                else
                {
                    outputAddress = Header.GenerateLink(Options.OutputMarkdownType, inputAddress);
                    var link = new Link(title, inputAddress, isRelative: true);
                    Logger?.Warn($"Link {link} is broken");
                }

                Link newLink = new Link(title, outputAddress, isRelative: true);
                linkString = newLink.ToString();
            }
            else if (isImage)
            {
                if (imageLinkNumber == 0 && !string.IsNullOrWhiteSpace(Options.HeaderImageLink))
                {
                    Link newLink = new Link(match.Value, Options.HeaderImageLink);
                    linkString = newLink.ToString();
                }
                else
                {
                    linkString = match.Value;
                }
                imageLinkNumber++;
            }
            else
            {
                linkString = match.Value;
            }

            return linkString;
        }

        private string ConvertDetailsElement(Match match)
        {
            if (string.IsNullOrEmpty(match.Groups[1].Value))
                return "";
            else
                return "</spoiler>";
        }

        private string ConvertSummaryElements(Match match)
        {
            return $"<spoiler title=\"{match.Groups[1].Value.Trim()}\">";
        }

        private string ConvertSpoilerOpenElement(Match match)
        {
            return  "<details>\n" +
                   $"<summary>{match.Groups[1].Value}</summary>";
        }

        private string ConvertSpolierCloseElement(Match match)
        {
            return "</details>";
        }
    }
}
