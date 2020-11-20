using System;
using Antlr4.Runtime.Tree;

namespace MarkConv.Nodes
{
    public class HtmlCommentNode : HtmlNode
    {
        public string Comment { get; }

        public HtmlCommentNode(ITerminalNode terminalNode, string comment, int start, int length)
            : base(terminalNode, start, length)
        {
            Comment = comment ?? throw new ArgumentNullException(nameof(comment));
        }
    }
}