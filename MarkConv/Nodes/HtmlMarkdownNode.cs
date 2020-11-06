using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace MarkConv.Nodes
{
    public class HtmlMarkdownNode : Node
    {
        public HtmlNode Object { get; }

        public HtmlMarkdownNode EndNode { get; }

        public HtmlMarkdownNode(HtmlNode htmlObject, HtmlMarkdownNode endNode, List<Node> children, int start, int length)
            : base(start, length)
        {
            Object = htmlObject ?? throw new ArgumentNullException(nameof(htmlObject));
            Children = children ?? throw new ArgumentNullException(nameof(children));
            EndNode = endNode;
        }

        public HtmlMarkdownNode(HtmlTextNode htmlTextNode, int start, int length)
            : base(start, length)
        {
            Object = htmlTextNode;
            Children = new List<Node>();
        }

        public override string ToString() => Object.ToString();
    }
}