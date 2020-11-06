using System;
using System.Collections.Generic;
using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public class MarkdownNode : Node
    {
        public MarkdownObject Object { get; }

        public MarkdownNode(MarkdownObject markdownObject, List<Node> children, int start, int length)
            : base(start, length)
        {
            Object = markdownObject ?? throw new ArgumentNullException(nameof(markdownObject));
            Children = children;
        }

        public override string ToString() => Object.ToString();
    }
}