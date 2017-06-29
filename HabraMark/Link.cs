namespace HabraMark
{
    public class Link
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public bool IsImage { get; set; }

        public bool IsRelative { get; set; }

        public Link()
        {
        }

        public Link(string title, string address)
        {
            Title = title;
            Address = address;
        }

        public override string ToString()
        {
            return $"{(IsImage ? "!" : "")}[{Title}]({(IsRelative ? "#" : "")}{Address})";
        }
    }
}
