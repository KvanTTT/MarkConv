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
        private int _spoilersLevel = 0;
        private bool _insideCodeBlock = false;
        private bool _insideComment = false;
        private int _imageLinkNumber;
        private List<Header> _headers;

        private readonly Dictionary<string, byte[]> _imageHashes = new Dictionary<string, byte[]>();
        private readonly Dictionary<string, bool> _urlStates = new Dictionary<string, bool>();

        public ProcessorOptions Options { get; }

        public ILogger Logger { get; set; }

        public LinksHtmlProcessor(ProcessorOptions options) => Options = options ?? new ProcessorOptions();

        public string Process(string text, List<Header> headers)
        {
            ReadOnlySpan<char> textSpan = text.AsSpan();

            _imageLinkNumber = 0;
            _headers = headers;
            var result = new StringBuilder(textSpan.Length);

            int index = 0;

            while (index < textSpan.Length)
            {
                var matches = new Dictionary<ElementType, Match>();
                Tuple<ElementType, Match> matchResult = null;

                while ((matchResult = NextMatch(text, index, matchResult, matches)) != null)
                {
                    ElementType elementType = matchResult.Item1;
                    Match match = matchResult.Item2;

                    if ((!Options.RemoveSpoilers || _spoilersLevel == 0) && (!Options.RemoveComments || !_insideComment))
                    {
                        result.Append(textSpan.Slice(index, match.Index - index));
                    }

                    string processedMatch = ProcessMatch(elementType, match);
                    result.Append(processedMatch);

                    index = match.Index + match.Length;

                    if (string.IsNullOrWhiteSpace(processedMatch))
                    {
                        while (index < textSpan.Length && char.IsWhiteSpace(textSpan[index]))
                            index++;
                        while (result.Length > 0 && SpaceChars.Contains(result[result.Length - 1]))
                            result.Remove(result.Length - 1, 1);
                    }
                }

                if ((!Options.RemoveSpoilers || _spoilersLevel == 0) && (!Options.RemoveComments || !_insideComment))
                {
                    result.Append(textSpan.Slice(index));
                }

                index = textSpan.Length;
            }

            return result.ToString();
        }

        private Tuple<ElementType, Match> NextMatch(string text, int index,
            Tuple<ElementType, Match> prevMatch, Dictionary<ElementType, Match> prevMatches)
        {
            if (prevMatch == null || prevMatch.Item1 == ElementType.CodeCloseElement)
            {
                prevMatches[ElementType.Link] = GetMatch(text, index, ElementType.Link);
                prevMatches[ElementType.HtmlLink] = GetMatch(text, index, ElementType.HtmlLink);

                if (Options.InputMarkdownType != MarkdownType.Habr &&
                    (Options.OutputMarkdownType == MarkdownType.Habr || Options.RemoveSpoilers))
                {
                    prevMatches[ElementType.DetailsOpenElement] =
                        GetMatch(text, index, ElementType.DetailsOpenElement);
                    prevMatches[ElementType.DetailsCloseElement] =
                        GetMatch(text, index, ElementType.DetailsCloseElement);
                    prevMatches[ElementType.SummaryElements] =
                        GetMatch(text, index, ElementType.SummaryElements);
                }

                if (Options.InputMarkdownType != MarkdownType.Common &&
                    (Options.OutputMarkdownType == MarkdownType.Common || Options.RemoveSpoilers))
                {
                    prevMatches[ElementType.SpoilerOpenElement] =
                        GetMatch(text, index, ElementType.SpoilerOpenElement);
                    prevMatches[ElementType.SpoilerCloseElement] =
                        GetMatch(text, index, ElementType.SpoilerCloseElement);
                    prevMatches[ElementType.AnchorElement] = GetMatch(text, index, ElementType.AnchorElement);
                }

                prevMatches[ElementType.CommentOpenElement] = GetMatch(text, index, ElementType.CommentOpenElement);
                prevMatches[ElementType.CodeOpenElement] = GetMatch(text, index, ElementType.CodeOpenElement);
            }
            else
            {
                if (prevMatch.Item1 == ElementType.CodeOpenElement)
                {
                    return new Tuple<ElementType, Match>(ElementType.CodeCloseElement, GetMatch(text, index, ElementType.CodeCloseElement));
                }

                if (prevMatch.Item1 == ElementType.CommentOpenElement)
                {
                    prevMatches.Remove(ElementType.CommentOpenElement);
                    prevMatches[ElementType.CommentCloseElement] = GetMatch(text, index, ElementType.CommentCloseElement);
                }
                else if (prevMatch.Item1 == ElementType.CommentCloseElement)
                {
                    prevMatches.Remove(ElementType.CommentCloseElement);
                    prevMatches[ElementType.CommentOpenElement] = GetMatch(text, index, ElementType.CommentOpenElement);
                }
                else
                {
                    prevMatches[prevMatch.Item1] = GetMatch(text, index, prevMatch.Item1);
                }
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

        private Match GetMatch(string text, int index, ElementType elementType)
        {
            if (ElementTypeRegex.TryGetValue(elementType, out Regex regex))
            {
                return regex.Match(text, index);
            }

            throw new NotImplementedException($"Regex for {elementType} has not been found");
        }

        private string ProcessMatch(ElementType elementType, Match match)
        {
            string result;

            switch (elementType)
            {
                case ElementType.Link:
                    result = ProcessLink(match);
                    break;

                case ElementType.DetailsOpenElement:
                    _spoilersLevel++;
                    result = "";
                    break;

                case ElementType.DetailsCloseElement:
                    _spoilersLevel--;
                    result = Options.RemoveSpoilers ? "" : "</spoiler>";
                    break;

                case ElementType.SummaryElements:
                    result = Options.RemoveSpoilers ? "" : $"<spoiler title=\"{match.Groups[1].Value.Trim()}\">";
                    break;

                case ElementType.SpoilerOpenElement:
                    _spoilersLevel++;
                    result = Options.RemoveSpoilers ? "" : $"<details>\n<summary>{match.Groups[1].Value}</summary>\n";
                    break;

                case ElementType.SpoilerCloseElement:
                    _spoilersLevel--;
                    result = Options.RemoveSpoilers ? "" : "</details>";
                    break;

                case ElementType.AnchorElement:
                    result = "";
                    break;

                case ElementType.HtmlLink:
                    result = $"src=\"{ProcessImageLink(match.Groups[1].Value)}\"";
                    break;

                case ElementType.CommentOpenElement:
                    _insideComment = true;
                    result = Options.RemoveComments ? "" : match.ToString();
                    break;

                case ElementType.CommentCloseElement:
                    _insideComment = false;
                    result = Options.RemoveComments ? "" : match.ToString();
                    break;

                case ElementType.CodeOpenElement:
                case ElementType.CodeCloseElement:
                    result = match.ToString();
                    break;

                default:
                    throw new NotImplementedException($"Regex for {elementType} is not supported");
            }

            if (Options.RemoveComments && _insideComment)
                return "";

            if (Options.RemoveSpoilers && _spoilersLevel > 0)
                return "";

            return result;
        }

        private string ProcessLink(Match match)
        {
            bool isImage = !string.IsNullOrEmpty(match.Groups[1].Value);
            string title = match.Groups[2].Value;
            string address = match.Groups[4].Value;
            LinkType linkType = Link.DetectLinkType(address);

            string linkString;
            if (Options.OutputMarkdownType != MarkdownType.Default && linkType == LinkType.Relative && !isImage)
            {
                string inputAddress = Header.GenerateLink(Options.InputMarkdownType, address);
                Header header = _headers.FirstOrDefault(h => h.Links[Options.InputMarkdownType].FullLink == inputAddress);
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
                if (_imageLinkNumber == 0 && !string.IsNullOrWhiteSpace(Options.HeaderImageLink))
                {
                    linkString = new Link(linkString, Options.HeaderImageLink).ToString();
                }

                _imageLinkNumber++;
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
