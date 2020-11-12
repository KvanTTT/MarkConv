using System;
using System.Collections.Generic;
using MarkConv.Links;
using MarkConv.Nodes;

namespace MarkConv
{
    public class ParseResult
    {
        public Node Node { get; }

        public IReadOnlyDictionary<Node, Link> Links { get; }

        public IReadOnlyDictionary<string, Anchor> Anchors { get; }

        public string EndOfLine { get; }

        public ParseResult(Node node, Dictionary<Node, Link> links, Dictionary<string, Anchor> anchors, string endOfLine)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
            Links = links ?? throw new ArgumentNullException(nameof(links));
            Anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
            EndOfLine = endOfLine ?? throw new ArgumentNullException(nameof(endOfLine));
        }
    }
}