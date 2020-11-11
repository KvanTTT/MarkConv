using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MarkConv.Html;

namespace MarkConv.Nodes
{
    public abstract class HtmlNode : Node
    {
        public IParseTree ParseTree { get; }

        protected HtmlNode(ParserRuleContext parserRuleContext)
            : base(((HtmlMarkdownToken)parserRuleContext.Start).File,
                parserRuleContext.Start.StartIndex, parserRuleContext.Stop.StopIndex - parserRuleContext.Start.StartIndex + 1)
        {
            ParseTree = parserRuleContext;
        }

        protected HtmlNode(ITerminalNode commentNode)
            : base(((HtmlMarkdownToken)commentNode.Symbol).File, commentNode.Symbol.StartIndex, commentNode.Symbol.StopIndex - commentNode.Symbol.StartIndex + 1)
        {
            ParseTree = commentNode;
        }

        protected HtmlNode(ITerminalNode commentNode, int start, int length)
            : base(((HtmlMarkdownToken)commentNode.Symbol).File, start, length)
        {
            ParseTree = commentNode;
        }
    }
}