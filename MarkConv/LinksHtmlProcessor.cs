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
        public const int HabrMaxTextLengthWithoutCut = 1000;
        public const int HabrMaxTextLengthBeforeCut = 2000;
        public const int HabrMinTextLengthBeforeCut = 100;
        public const int HabrMinTextLengthAfterCut = 100;

        public static readonly string HabrMaxTextLengthWithoutCutMessage =
            $"You need to insert <cut/> tag if the text contains more than {HabrMaxTextLengthWithoutCut} characters";
        public static readonly string HabrMaxTextLengthBeforeCutMessage =
            $"Text before cut can not be more than or equal to {HabrMaxTextLengthBeforeCut} characters";
        public static readonly string HabrMinTextLengthBeforeCutMessage =
            $"Text before cut can not be less than {HabrMinTextLengthBeforeCut} characters";
        public static readonly string HabrMinTextLengthAfterCutMessage =
            $"Text after cut can not be less than {HabrMinTextLengthAfterCut} characters";

        private int _spoilersLevel;
        private bool _insideComment;
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
            int cutElementIndex = -1;

            while (index < textSpan.Length)
            {
                var matches = new Dictionary<ElementType, Match>();
                MatchResult matchResult = NextMatch(text, index, null, matches);

                while (matchResult != null)
                {
                    Match match = matchResult.Match;

                    if ((!Options.RemoveSpoilers || _spoilersLevel == 0) && (!Options.RemoveComments || !_insideComment))
                    {
                        result.Append(textSpan.Slice(index, match.Index - index));
                    }

                    index = match.Index + match.Length;

                    if (matchResult.Type == ElementType.CutElement)
                    {
                        if (cutElementIndex == -1)
                        {
                            cutElementIndex = result.Length;
                        }
                    }

                    string processedMatch;
                    (processedMatch, matchResult) = ProcessMatch(matchResult, text, matches);
                    result.Append(processedMatch);

                    if (string.IsNullOrWhiteSpace(processedMatch))
                    {
                        while (index < textSpan.Length && char.IsWhiteSpace(textSpan[index]))
                            index++;
                        while (result.Length > 0 && SpaceChars.Contains(result[^1]))
                            result.Remove(result.Length - 1, 1);
                    }
                }

                if ((!Options.RemoveSpoilers || _spoilersLevel == 0) && (!Options.RemoveComments || !_insideComment))
                {
                    result.Append(textSpan.Slice(index));
                }

                index = textSpan.Length;
            }

            string resultStr = result.ToString();

            if (Options.OutputMarkdownType == MarkdownType.Habr)
            {
                if (cutElementIndex == -1)
                {
                    if (resultStr.Trim().Length >= HabrMaxTextLengthWithoutCut)
                    {
                        Logger?.Warn(HabrMaxTextLengthWithoutCutMessage);
                    }
                }
                else
                {
                    if (cutElementIndex > HabrMaxTextLengthBeforeCut)
                    {
                        Logger?.Warn(HabrMaxTextLengthBeforeCutMessage);
                    }
                    else if (cutElementIndex < HabrMinTextLengthBeforeCut)
                    {
                        Logger?.Warn(HabrMinTextLengthBeforeCutMessage);
                    }

                    if (result.Length - (cutElementIndex + 4) < HabrMinTextLengthAfterCut) // TODO: Bug on habr.com
                    {
                        Logger?.Warn(HabrMinTextLengthAfterCutMessage);
                    }
                }
            }

            return resultStr;
        }

        private MatchResult NextMatch(string text, int index,
            MatchResult prevMatch, Dictionary<ElementType, Match> prevMatches)
        {
            if (prevMatch == null || prevMatch.Type == ElementType.CodeElement || prevMatch.Type == ElementType.CodeCloseElement)
            {
                prevMatches[ElementType.Link] = GetMatch(text, index, ElementType.Link);
                prevMatches[ElementType.HtmlLink] = GetMatch(text, index, ElementType.HtmlLink);

                if (Options.InputMarkdownType != MarkdownType.Habr &&
                    (Options.OutputMarkdownType == MarkdownType.Habr || Options.OutputMarkdownType == MarkdownType.Dev || Options.RemoveSpoilers))
                {
                    prevMatches[ElementType.DetailsOpenElement] =
                        GetMatch(text, index, ElementType.DetailsOpenElement);
                    prevMatches[ElementType.DetailsCloseElement] =
                        GetMatch(text, index, ElementType.DetailsCloseElement);
                    prevMatches[ElementType.SummaryElements] =
                        GetMatch(text, index, ElementType.SummaryElements);
                }

                if (Options.InputMarkdownType == MarkdownType.Habr &&
                    (Options.OutputMarkdownType == MarkdownType.GitHub || Options.OutputMarkdownType == MarkdownType.Dev || Options.RemoveSpoilers))
                {
                    prevMatches[ElementType.SpoilerOpenElement] =
                        GetMatch(text, index, ElementType.SpoilerOpenElement);
                    prevMatches[ElementType.SpoilerCloseElement] =
                        GetMatch(text, index, ElementType.SpoilerCloseElement);
                    prevMatches[ElementType.AnchorElement] = GetMatch(text, index, ElementType.AnchorElement);
                }

                if (Options.OutputMarkdownType == MarkdownType.Habr)
                {
                    prevMatches[ElementType.CutElement] = GetMatch(text, index, ElementType.CutElement);
                }

                prevMatches[ElementType.CodeElement] = GetMatch(text, index, ElementType.CodeElement);
                prevMatches[ElementType.CommentOpenElement] = GetMatch(text, index, ElementType.CommentOpenElement);
                prevMatches[ElementType.CodeOpenElement] = GetMatch(text, index, ElementType.CodeOpenElement);
            }
            else
            {
                if (prevMatch.Type == ElementType.CodeOpenElement)
                {
                    return new MatchResult(ElementType.CodeCloseElement, GetMatch(text, index, ElementType.CodeCloseElement));
                }

                if (prevMatch.Type == ElementType.CommentOpenElement)
                {
                    prevMatches.Remove(ElementType.CommentOpenElement);
                    prevMatches[ElementType.CommentCloseElement] = GetMatch(text, index, ElementType.CommentCloseElement);
                }
                else if (prevMatch.Type == ElementType.CommentCloseElement)
                {
                    prevMatches.Remove(ElementType.CommentCloseElement);
                    prevMatches[ElementType.CommentOpenElement] = GetMatch(text, index, ElementType.CommentOpenElement);
                }
                else
                {
                    prevMatches[prevMatch.Type] = GetMatch(text, index, prevMatch.Type);
                }
            }

            MatchResult result = null;
            foreach (KeyValuePair<ElementType, Match> match in prevMatches)
            {
                if (match.Value.Success && match.Value.Index < (result?.Match.Index ?? int.MaxValue))
                {
                    result = new MatchResult(match.Key, match.Value);
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

        private (string, MatchResult) ProcessMatch(MatchResult matchResult, string text, Dictionary<ElementType, Match> matches)
        {
            string result;
            var match = matchResult.Match;
            var nextMatch = NextMatch(text, match.Index + match.Length, matchResult, matches);

            switch (matchResult.Type)
            {
                case ElementType.Link:
                    result = ProcessLink(match);
                    break;

                case ElementType.DetailsOpenElement:
                    _spoilersLevel++;
                    result = Options.RemoveSpoilers
                        ? ""
                        : nextMatch.Type == ElementType.SummaryElements
                            ? ""
                            : Options.OutputMarkdownType == MarkdownType.Habr
                                ? "<spoiler>"
                                : Options.OutputMarkdownType == MarkdownType.Dev
                                    ? "{% details %}"
                                    : "";
                    break;

                case ElementType.DetailsCloseElement:
                    _spoilersLevel--;
                    result = Options.RemoveSpoilers
                        ? ""
                        : Options.OutputMarkdownType == MarkdownType.Habr
                            ? "</spoiler>"
                            : Options.OutputMarkdownType == MarkdownType.Dev
                                ? "{% enddetails %}"
                                : "";
                    break;

                case ElementType.SummaryElements:
                    string summary = match.Groups[1].Value.Trim();
                    result = Options.RemoveSpoilers
                        ? ""
                        : Options.OutputMarkdownType == MarkdownType.Habr
                            ? $"<spoiler title=\"{summary}\">"
                            : Options.OutputMarkdownType == MarkdownType.Dev
                                ? $"{{% details {summary} %}}"
                                : "";
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
                    result = $"src=\"{ProcessImageAddress(match.Groups[1].Value)}\"{match.Groups[2].Value}";
                    break;

                case ElementType.CommentOpenElement:
                    _insideComment = true;
                    result = Options.RemoveComments ? "" : match.ToString();
                    break;

                case ElementType.CommentCloseElement:
                    _insideComment = false;
                    result = Options.RemoveComments ? "" : match.ToString();
                    break;

                case ElementType.CodeElement:
                case ElementType.CodeOpenElement:
                case ElementType.CodeCloseElement:
                case ElementType.CutElement:
                    result = match.ToString();
                    break;

                default:
                    throw new NotImplementedException($"Regex for {matchResult.Type} is not supported");
            }

            if (Options.RemoveComments && _insideComment)
                result = "";

            if (Options.RemoveSpoilers && _spoilersLevel > 0)
                result = "";

            return (result, nextMatch);
        }

        private string ProcessLink(Match match)
        {
            bool isImage = !string.IsNullOrEmpty(match.Groups[1].Value);
            string title = match.Groups[2].Value;
            string address = match.Groups[4].Value;
            LinkType linkType = Link.DetectLinkType(address);

            string linkString;
            if (linkType == LinkType.Relative && !isImage)
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
                string newAddress = ProcessImageAddress(address);

                linkString = Options.CenterImageAlignment
                    ? $"<img src=\"{newAddress}\" align=center />"
                    : new Link(title, newAddress, true).ToString();

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
                    if (!_urlStates.TryGetValue(address, out bool isAlive))
                    {
                        isAlive = Link.IsUrlAlive(address);
                        _urlStates.Add(address, isAlive);
                    }

                    if (!isAlive)
                    {
                        Logger?.Warn($"Link {address} is probably broken");
                    }
                }
            }

            return linkString;
        }

        private string ProcessImageAddress(string address)
        {
            byte[] hash = null;
            address = address.Trim('"', '\'');
            string newLink = address;

            if (Options.CheckLinks)
            {
                if (!_imageHashes.TryGetValue(address, out hash))
                {
                    hash = Link.GetImageHash(address, Options.RootDirectory);
                    _imageHashes.Add(address, hash);
                }

                if (hash == null)
                {
                    LinkType linkType = Link.DetectLinkType(address);
                    string warnMessage = linkType == LinkType.Local
                        ? $"File {address} does not exist"
                        : $"Link {address} is probably broken";
                    Logger?.Warn(warnMessage);
                }
            }

            if (Options.ImagesMap.TryGetValue(address, out ImageHash replacement))
            {
                if (Options.CheckLinks && replacement.Hash.Value == null)
                {
                    Logger?.Warn($"Replacement link {replacement.Path} is probably broken");
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
