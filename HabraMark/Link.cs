using System.Text.RegularExpressions;

namespace HabraMark
{
    public class Link
    {
        public string Title { get; set; }

        public string Address { get; set; }

        public bool IsImage { get; set; }

        public bool IsRelative { get; set; }

        public int Index { get; set; }

        public int Length { get; set; }

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

        public static Link ParseNextLink(string text, int index, int length)
        {
            Match match = MdRegex.LinkRegex.Match(text, index, length);
            if (match.Success)
            {
                Link link = new Link
                {
                    IsImage = !string.IsNullOrEmpty(match.Groups[1].Value),
                    Title = match.Groups[2].Value,
                    IsRelative = !string.IsNullOrEmpty(match.Groups[4].Value),
                    Address = match.Groups[5].Value,
                    Index = match.Index,
                    Length = match.Length,
                };
                return link;
            }
            return null;
        }
    }
}
