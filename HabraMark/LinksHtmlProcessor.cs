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

                string textFragment = text.Substring(index, startCodeFragmentIndex - index);
                textFragment = ProcessElements(textFragment, ElementType.Link);
                if (Options.ReplaceSpoilers)
                {
                    textFragment = ProcessElements(textFragment, ElementType.DetailsElement);
                    textFragment = ProcessElements(textFragment, ElementType.SummaryElements);
                }

                result.Append(textFragment);

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

        private string ProcessElements(string text, ElementType elementType)
        {
            StringBuilder result = null;
            int index = 0;
            Match match;
            Regex regex
                = elementType == ElementType.Link ? LinkRegex
                : elementType == ElementType.DetailsElement ? DetailsTagRegex
                : elementType == ElementType.SummaryElements ? SummaryTagsRegex
                : null;
            while ((match = regex.Match(text, index)).Success)
            {
                if (result == null)
                {
                    result = new StringBuilder(text.Length);
                }
                result.Append(text.Substring(index, match.Index - index));

                string processedMatch
                    = elementType == ElementType.Link ? ProcessLink(_headers, ref _imageLinkNumber, match)
                    : elementType == ElementType.DetailsElement ? ProcessDetailsElement(match)
                    : elementType == ElementType.SummaryElements ? ProcessSummaryElements(match)
                    : "";

                result.Append(processedMatch);
                index = match.Index + match.Length;

                if (string.IsNullOrWhiteSpace(processedMatch) && index < text.Length && text[index] == '\n')
                {
                    index++;
                }
            }
            if (index == 0)
                return text;

            result.Append(text.Substring(index));
            return result.ToString();
        }

        private string ProcessLink(List<Header> headers, ref int imageLinkNumber, Match match)
        {
            bool isImage = !string.IsNullOrEmpty(match.Groups[1].Value);
            string title = match.Groups[2].Value;
            bool isRelative = !string.IsNullOrEmpty(match.Groups[4].Value);
            string address = match.Groups[5].Value;

            string linkString;
            if (Options.OutputRelativeLinksKind != RelativeLinksKind.Default && isRelative && !isImage)
            {
                Header header;
                string inputAddress = Header.GetAppropriateLink(Options.InputRelativeLinksKind, address);
                string outputAddress =
                    (header = headers.FirstOrDefault(h => h.GetAppropriateLink(Options.InputRelativeLinksKind) == inputAddress)) != null
                    ? header.GetAppropriateLink(Options.OutputRelativeLinksKind)
                    : Header.GetAppropriateLink(Options.OutputRelativeLinksKind, inputAddress);

                Link newLink = new Link(title, outputAddress) { IsRelative = true };
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

        private string ProcessDetailsElement(Match match)
        {
            if (string.IsNullOrEmpty(match.Groups[1].Value))
                return "";
            else
                return "</spoiler>";
        }

        private string ProcessSummaryElements(Match match)
        {
            return $"<spoiler title=\"{match.Groups[1].Value.Trim()}\">";
        }
    }
}
