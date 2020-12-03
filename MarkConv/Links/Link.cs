using System.Text.RegularExpressions;
using MarkConv.Nodes;

namespace MarkConv.Links
{
    public abstract class Link
    {
        private static readonly Regex UrlRegex = new Regex(
            @"https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,}",
            RegexOptions.Compiled);

        public Node Node { get; }

        public string Address { get; }

        public bool IsImage { get; }

        public int Start { get; }

        public int Length { get; }

        public static Link Create(Node node, string address, bool isImage = false, int start = -1,
            int length = -1)
        {
            address = address.Trim();
            if (UrlRegex.IsMatch(address))
                return new AbsoluteLink(node, address, isImage, start, length);

            if (address.StartsWith("#"))
                return new RelativeLink(node, address.Substring(1), start, length);

            return new LocalLink(node, address, isImage, start, length);
        }

        protected Link(Node node, string address, bool isImage = false, int start = -1, int length = -1)
        {
            Node = node;
            Address = address;
            IsImage = isImage;
            Start = start == -1 ? node.Start : start;
            Length = length == -1 ? node.Length : length;
        }

        public override string ToString() => Address;
    }
}
