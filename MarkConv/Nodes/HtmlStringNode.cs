using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace MarkConv.Nodes
{
    public class HtmlStringNode : HtmlNode
    {
        public string String { get; }

        public HtmlStringNode(ITerminalNode terminalNode, string value, int start, int length)
            : base(terminalNode, start, length)
        {
            String = value ?? throw new ArgumentNullException(nameof(value));
        }

        public HtmlStringNode(ITerminalNode terminalNode)
            : base(terminalNode)
        {
            String = terminalNode.Symbol.Text;
        }

        public HtmlStringNode(ParserRuleContext parseRuleContext)
            : base(parseRuleContext)
        {
            String = Substring;
        }

        public override string ToString() => String;
    }
}