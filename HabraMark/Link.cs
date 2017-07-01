namespace HabraMark
{
    public class Link
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public bool IsImage { get; set; }

        public bool IsRelative { get; set; }

        public Link(string title, string address, bool isImage = false, bool isRelative = false)
        {
            Title = title;
            Address = address;
            IsImage = isImage;
            IsRelative = isRelative;
        }

        public override string ToString()
        {
            return $"{(IsImage ? "!" : "")}[{Title}]({(IsRelative ? "#" : "")}{Address})";
        }
    }
}
