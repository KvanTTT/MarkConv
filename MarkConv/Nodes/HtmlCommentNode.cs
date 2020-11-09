using System;
using Antlr4.Runtime.Tree;

namespace MarkConv.Nodes
{
    public class HtmlCommentNode : HtmlNode
    {
        public string Comment { get; }

        public HtmlCommentNode(ITerminalNode commentNode, string comment, int start, int length)
            : base(commentNode, start, length)
        {
            Comment = comment ?? throw new ArgumentNullException(nameof(comment));
        }
    }
}