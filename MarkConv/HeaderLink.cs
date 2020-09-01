using System;

namespace MarkConv
{
    public class HeaderLink
    {
        public string Link { get; }

        public int LinkNumber { get; }

        public string FullLink => LinkNumber == 0 ? Link : $"{Link}-{LinkNumber}";

        public HeaderLink(string link, int linkNumber)
        {
            Link = link ?? throw new ArgumentNullException(nameof(link));
            LinkNumber = linkNumber;
        }

        public override string ToString() => FullLink;
    }
}
