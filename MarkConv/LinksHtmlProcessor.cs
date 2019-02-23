using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static MarkConv.MarkdownRegex;

namespace MarkConv
{
    public class LinksHtmlProcessor
    {
        private int _imageLinkNumber;
        private List<Header> _headers;

        private Dictionary<string, byte[]> _imageHashes = new Dictionary<string, byte[]>();
        private Dictionary<string, bool> _urlStates = new Dictionary<string, bool>();

        public ProcessorOptions Options { get; set; }

        public ILogger Logger { get; set; }

        public LinksHtmlProcessor(ProcessorOptions options) => Options = options ?? new ProcessorOptions();

        public string Process(string text, List<Header> headers)
        {
            _imageLinkNumber = 0;
            _headers = headers;
            var result = new StringBuilder(text.Length);

            int spoilersLevel = 0;
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

                    string processedMatch = null;

                    if (!Options.RemoveSpoilers || spoilersLevel == 0)
                    {
                        result.Append(text.Substring(index, match.Index - index));

                        processedMatch
                            = elementType == ElementType.Link ? ProcessLink(_headers, ref _imageLinkNumber, match)
                            : elementType == ElementType.DetailsOpenElement ? ConvertDetailsToSpoilerOpen(match)
                            : elementType == ElementType.DetailsCloseElement ? ConvertDetailsToSpoilerClose(match)
                            : elementType == ElementType.SummaryElements ? ConvertSummaryToSpoilerOpen(match)
                            : elementType == ElementType.SpoilerOpenElement ? ConvertSpoilerToDetailsOpen(match)
                            : elementType == ElementType.SpoilerCloseElement ? ConvertSpoilerToDetailsClose(match)
                            : elementType == ElementType.AnchorElement ? ConvertAnchorElement(match)
                            : elementType == ElementType.HtmlLink ? ConvertHtmlLink(match)
                            : elementType == ElementType.CommentElement ? ConvertComment(match)
                            : "";

                        result.Append(processedMatch);
                    }

                    if (elementType == ElementType.SpoilerOpenElement || elementType == ElementType.DetailsOpenElement)
                        spoilersLevel++;
                    else if (elementType == ElementType.SpoilerCloseElement || elementType == ElementType.DetailsCloseElement)
                        spoilersLevel--;

                    index = match.Index + match.Length;

                    if (string.IsNullOrWhiteSpace(processedMatch))
                    {
                        while (index < text.Length && char.IsWhiteSpace(text[index]))
                            index++;
                        while (result.Length > 0 && SpaceChars.Contains(result[result.Length - 1]))
                            result.Remove(result.Length - 1, 1);
                    }
                }

                if (!Options.RemoveSpoilers || spoilersLevel == 0)
                {
                    result.Append(text.Substring(index, startCodeFragmentIndex - index));
                }

                if (startCodeFragmentMatch.Success)
                {
                    Match endCodeFragmentMatch = CodeSectionRegex.Match(text, startCodeFragmentMatch.Index + startCodeFragmentMatch.Length);
                    index = endCodeFragmentMatch.Success
                        ? endCodeFragmentMatch.Index + endCodeFragmentMatch.Length
                        : text.Length;

                    if (!Options.RemoveSpoilers || spoilersLevel == 0)
                    {
                        result.Append(
                            text.Substring(startCodeFragmentMatch.Index, index - startCodeFragmentMatch.Index));
                    }
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
                prevMatches[ElementType.HtmlLink] = GetMatch(text, index, length, ElementType.HtmlLink);
                if (Options.InputMarkdownType != MarkdownType.Habr &&
                    (Options.OutputMarkdownType == MarkdownType.Habr || Options.RemoveSpoilers))
                {
                    prevMatches[ElementType.DetailsOpenElement] =
                        GetMatch(text, index, length, ElementType.DetailsOpenElement);
                    prevMatches[ElementType.DetailsCloseElement] =
                        GetMatch(text, index, length, ElementType.DetailsCloseElement);
                    prevMatches[ElementType.SummaryElements] =
                        GetMatch(text, index, length, ElementType.SummaryElements);
                }

                if (Options.InputMarkdownType != MarkdownType.GitHub &&
                    Options.InputMarkdownType != MarkdownType.VisualCode &&
                    (Options.OutputMarkdownType == MarkdownType.GitHub ||
                     Options.OutputMarkdownType == MarkdownType.VisualCode || Options.RemoveSpoilers))
                {
                    prevMatches[ElementType.SpoilerOpenElement] =
                        GetMatch(text, index, length, ElementType.SpoilerOpenElement);
                    prevMatches[ElementType.SpoilerCloseElement] =
                        GetMatch(text, index, length, ElementType.SpoilerCloseElement);
                    prevMatches[ElementType.AnchorElement] = GetMatch(text, index, length, ElementType.AnchorElement);
                }

                prevMatches[ElementType.CommentElement] = GetMatch(text, index, length, ElementType.CommentElement);
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
                : elementType == ElementType.DetailsOpenElement ? DetailsOpenTagRegex
                : elementType == ElementType.DetailsCloseElement ? DetailsCloseTagRegex
                : elementType == ElementType.SummaryElements ? SummaryTagsRegex
                : elementType == ElementType.SpoilerOpenElement ? SpoilerOpenTagRegex
                : elementType == ElementType.SpoilerCloseElement ? SpoilerCloseTagRegex
                : elementType == ElementType.AnchorElement ? AnchorTagRegex
                : elementType == ElementType.HtmlLink ? SrcUrlRegex
                : elementType == ElementType.CommentElement ? CommentRegex
                : throw new NotImplementedException($"Regex for {elementType} has not been found");

            return regex.Match(text, index, length);
        }

        private string ProcessLink(List<Header> headers, ref int imageLinkNumber, Match match)
        {
            bool isImage = !string.IsNullOrEmpty(match.Groups[1].Value);
            string title = match.Groups[2].Value;
            string address = match.Groups[4].Value;
            LinkType linkType = Link.DetectLinkType(address);

            string linkString;
            if (Options.OutputMarkdownType != MarkdownType.Default && linkType == LinkType.Relative && !isImage)
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
                    var link = new Link(title, inputAddress, linkType: LinkType.Relative);
                    Logger?.Warn($"Link {link} is broken");
                }

                Link newLink = new Link(title, outputAddress, linkType: LinkType.Relative);
                linkString = newLink.ToString();
            }
            else if (isImage)
            {
                string newLink = ProcessImageLink(address);

                linkString = new Link(title, newLink, true).ToString();
                if (imageLinkNumber == 0 && !string.IsNullOrWhiteSpace(Options.HeaderImageLink))
                {
                    linkString = new Link(linkString, Options.HeaderImageLink).ToString();
                }

                imageLinkNumber++;
                return linkString;
            }
            else
            {
                linkString = match.Value;
                if (Options.CheckLinks)
                {
                    if (!_urlStates.TryGetValue(address, out bool isValid))
                    {
                        isValid = Link.IsUrlValid(address);
                        _urlStates[address] = isValid;
                    }

                    if (!isValid)
                    {
                        Logger?.Warn($"Link {address} probably broken");
                    }
                }
            }

            return linkString;
        }

        private string ConvertSummaryToSpoilerOpen(Match match)
        {
            return Options.RemoveSpoilers ? "" : $"<spoiler title=\"{match.Groups[1].Value.Trim()}\">";
        }

        private string ConvertDetailsToSpoilerOpen(Match match)
        {
            return "";
        }

        private string ConvertDetailsToSpoilerClose(Match match)
        {
            return Options.RemoveSpoilers ? "" : "</spoiler>";
        }

        private string ConvertSpoilerToDetailsOpen(Match match)
        {
            return Options.RemoveSpoilers ? "" : $"<details>\n<summary>{match.Groups[1].Value}</summary>\n";
        }

        private string ConvertSpoilerToDetailsClose(Match match)
        {
            return Options.RemoveSpoilers ? "" : "</details>";
        }

        private string ConvertAnchorElement(Match match)
        {
            return "";
        }

        private string ConvertHtmlLink(Match match)
        {
            return $"src=\"{ProcessImageLink(match.Groups[1].Value)}\"";
        }

        private string ConvertComment(Match match)
        {
            return Options.RemoveComments ? "" : match.ToString();
        }

        private string ProcessImageLink(string address)
        {
            byte[] hash = null;
            address = address.Trim('"');
            string newLink = address;
            if (Options.CheckLinks)
            {
                if (!_imageHashes.TryGetValue(address, out hash))
                {
                    hash = Link.GetImageHash(address, Options.RootDirectory);
                    _imageHashes[address] = hash;
                }

                if (hash == null)
                {
                    LinkType linkType = Link.DetectLinkType(address);
                    string warnMessage = linkType == LinkType.Local
                        ? $"File {address} does not exist"
                        : $"Link {address} probably broken";
                    Logger?.Warn(warnMessage);
                }
            }

            if (Options.ImagesMap.TryGetValue(address, out ImageHash replacement))
            {
                if (Options.CheckLinks && replacement.Hash.Value == null)
                {
                    Logger?.Warn($"Replacement link {replacement.Path} probably broken");
                }
                if (Options.CompareImageHashes &&
                    hash != null && replacement.Hash.Value != null && !Link.CompareHashes(hash, replacement.Hash.Value))
                {
                    Logger?.Warn($"Images {address} and {replacement.Path} are different");
                }
                newLink = replacement.Path;
            }

            return newLink;
        }
    }
}
