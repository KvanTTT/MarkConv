using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace MarkConv.Nodes
{
    public abstract class HtmlNode : Node
    {
        public IParseTree ParseTree { get; }

        protected HtmlNode(ParserRuleContext parserRuleContext)
            : base(parserRuleContext.Start.StartIndex, parserRuleContext.Stop.StopIndex - parserRuleContext.Start.StartIndex + 1)
        {
            ParseTree = parserRuleContext;
        }

        protected HtmlNode(ITerminalNode commentNode)
            : base(commentNode.Symbol.StartIndex, commentNode.Symbol.StopIndex - commentNode.Symbol.StartIndex + 1)
        {
            ParseTree = commentNode;
        }

        protected HtmlNode(ITerminalNode commentNode, int start, int length)
            : base(start, length)
        {
            ParseTree = commentNode;
        }
    }
}