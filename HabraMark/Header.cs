using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HabraMark
{
    public class Header
    {
        private static readonly Dictionary<char, string> HabraHeaderReplacement = new Dictionary<char, string>
        {
            ['а'] = "a",
            ['б'] = "b",
            ['в'] = "v",
            ['г'] = "g",
            ['д'] = "d",
            ['е'] = "e",
            ['ё'] = "yo",
            ['ж'] = "zh",
            ['з'] = "z",
            ['и'] = "i",
            ['й'] = "y",
            ['к'] = "k",
            ['л'] = "l",
            ['м'] = "m",
            ['н'] = "n",
            ['о'] = "o",
            ['п'] = "p",
            ['р'] = "r",
            ['с'] = "s",
            ['т'] = "t",
            ['у'] = "u",
            ['ф'] = "f",
            ['х'] = "h",
            ['ц'] = "c",
            ['ч'] = "ch",
            ['ш'] = "sh",
            ['щ'] = "sch",
            ['ы'] = "y",
            ['э'] = "e",
            ['ю'] = "yu",
            ['я'] = "ya",
            ['-'] = "-",
            ['_'] = "_",
            [' '] = "-"
        };

        public string Text { get; set; } = "";

        public int Level { get; set; } = 1;

        public string Link { get; set; } = "";

        public int LinkNumber { get; set; } = 0;

        public string FullLink
        {
            get
            {
                return LinkNumber == 0 ? Link : $"{Link}-{LinkNumber}";
            }
        }

        public string LoweredLink { get; set; } = "";

        public int LoweredLinkNumber { get; set; } = 0;

        public string FullLoweredLink
        {
            get
            {
                return LoweredLinkNumber == 0 ? LoweredLink : $"{LoweredLink}-{LoweredLinkNumber}";
            }
        }

        public string HabraLink { get; set; } = "";

        public int HabraLinkNumber { get; set; } = 0;

        public string FullHabraLink
        {
            get
            {
                return HabraLinkNumber == 0 ? HabraLink : $"{HabraLink}-{HabraLinkNumber}";
            }
        }

        public Header(string text, int level)
        {
            Text = text;
            Level = level;
        }

        public string GetAppropriateLink(MarkdownType kind)
        {
            switch (kind)
            {
                case MarkdownType.GitHub:
                    return FullLink;
                case MarkdownType.Habrahabr:
                    return FullHabraLink;
                case MarkdownType.VisualCode:
                default:
                    return FullLoweredLink;
            }
        }

        public override string ToString()
        {
            return $"{new string('#', Level)} {Text}";
        }

        public static string GetAppropriateLink(MarkdownType kind, string inputLink)
        {
            switch (kind)
            {
                case MarkdownType.GitHub:
                    return HeaderToLink(inputLink, false);
                case MarkdownType.Habrahabr:
                    return HeaderToHabralink(inputLink);
                case MarkdownType.VisualCode:
                default:
                    return HeaderToLink(inputLink, true);
            }
        }

        public static void AddHeader(List<Header> headers, string header, int level)
        {
            string headerLink = HeaderToLink(header, false);
            string loweredHeaderLink = HeaderToLink(header, true);
            string headerHabraLink = HeaderToHabralink(header);
            headers.Add(new Header(header, level)
            {
                Link = headerLink,
                LinkNumber = headers.Count(h => h.Link == headerLink),
                LoweredLink = loweredHeaderLink,
                LoweredLinkNumber = headers.Count(h => h.LoweredLink == loweredHeaderLink),
                HabraLink = headerHabraLink,
                HabraLinkNumber = headers.Count(h => h.HabraLink == headerHabraLink)
            });
        }

        public static string HeaderToLink(string header, bool lower)
        {
            var link = new StringBuilder(header.Length);
            foreach (char c in header)
            {
                if (char.IsLetterOrDigit(c))
                {
                    link.Append(lower || (c >= 'A' && c <= 'Z') ? char.ToLowerInvariant(c) : c);
                }
                else
                {
                    if (c == ' ' || c == '-')
                        link.Append('-');
                    else if (c == '_')
                        link.Append('_');
                }
            }
            return link.ToString();
        }

        public static string HeaderToHabralink(string header)
        {
            string lower = header.ToLowerInvariant();
            var link = new StringBuilder(lower.Length);
            foreach (char c in lower)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    link.Append(c);
                }
                else if (HabraHeaderReplacement.TryGetValue(c, out string replacement))
                {
                    link.Append(replacement);
                }
            }
            return link.ToString();
        }
    }
}
