using System;
using Antlr4.Runtime.Tree;

namespace MarkConv.Nodes
{
    public class HtmlStringNode : HtmlNode
    {
        public string String { get; }

        public HtmlStringNode(ITerminalNode commentNode, string value, int start, int length)
            : base(commentNode, start, length)
        {
            String = value ?? throw new ArgumentNullException(nameof(value));
        }

        public HtmlStringNode(ITerminalNode commentNode)
            : base(commentNode)
        {
            String = commentNode.Symbol.Text;
        }

        public override string ToString() => String;
    }
}