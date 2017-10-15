namespace MarkConv
{
    public class HeaderLink
    {
        public string Link { get; set; }

        public int LinkNumber { get; set; }

        public string FullLink => LinkNumber == 0 ? Link : $"{Link}-{LinkNumber}";

        public HeaderLink(string link, int linkNumber)
        {
            Link = link;
            LinkNumber = linkNumber;
        }

        public override string ToString() => FullLink;
    }
}
