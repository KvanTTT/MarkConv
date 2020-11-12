using System;
using System.Dynamic;
using MarkConv.Links;
using MarkConv.Nodes;

namespace MarkConv.Links
{
    public abstract class Link
    {
        public Node Node { get; }

        public string Address { get; }

        public bool IsImage { get; }

        public int Start { get; }

        public int Length { get; }

        public static Link Create(Node node, string address, bool isImage = false, int start = -1,
            int length = -1)
        {
            address = address.Trim();
            if (Consts.UrlRegex.IsMatch(address))
                return new AbsoluteLink(node, address, isImage, start, length);

            if (address.StartsWith("#"))
                return new RelativeLink(node, address, start, length);

            return new LocalLink(node, address, isImage, start, length);
        }

        protected Link(Node node, string address, bool isImage = false, int start = -1, int length = -1)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            IsImage = isImage;
            Start = start == -1 ? node.Start : start;
            Length = length == -1 ? node.Length : length;
        }

        public override string ToString() => Address;
    }
}
