namespace HabraMark
{
    public class Link
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public bool IsImage { get; set; }

        public LinkType LinkType { get; set; }

        public Link(string title, string address, bool isImage = false, LinkType linkType = LinkType.Absolute)
        {
            Title = title;
            Address = address;
            IsImage = isImage;
            LinkType = linkType;
        }

        public static LinkType DetectLinkType(string address)
        {
            address = address.Trim();
            if (MarkdownRegex.UrlRegex.IsMatch(address))
                return LinkType.Absolute;

            if (address.StartsWith("#"))
                return LinkType.Relative;

            return LinkType.Local;
        }

        public override string ToString()
        {
            return $"{(IsImage ? "!" : "")}[{Title}]({(LinkType == LinkType.Relative ? "#" : "")}{Address})";
        }
    }
}
