using System;
using MarkConv.Nodes;

namespace MarkConv
{
    public class Link
    {
        public Node Node { get; }

        public string Address { get; }

        public bool IsImage { get; }

        public LinkType LinkType { get; }

        public int Start { get; }

        public int Length { get; }

        public Link(Node node, string address, bool isImage = false, LinkType linkType = LinkType.Absolute,
            int start = -1, int length = -1)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            IsImage = isImage;
            LinkType = linkType;
            Start = start == -1 ? node.Start : start;
            Length = length == -1 ? node.Length : length;
        }

        public override string ToString() => Address;
    }
}
