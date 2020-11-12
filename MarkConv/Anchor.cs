using System;
using MarkConv.Nodes;

namespace MarkConv
{
    public class Anchor
    {
        public Node Node { get; }

        public string Title { get; }

        public string Address { get; }

        public int Number { get; }

        public Anchor(Node node, string title, string address, int number)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Number = number;
        }

        public override string ToString() => $"{Address}; {Title}";
    }
}