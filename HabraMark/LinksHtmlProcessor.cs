using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static HabraMark.MdRegex;

namespace HabraMark
{
    public class LinksHtmlProcessor
    {
        public ProcessorOptions Options { get; set; }

        public LinksHtmlProcessor(ProcessorOptions options) => Options = options ?? new ProcessorOptions();

        public string Process(string text, List<Header> headers)
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
    }
}
