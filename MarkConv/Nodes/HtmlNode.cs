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

        protected HtmlNode(ITerminalNode terminalNode)
            : base(((HtmlMarkdownToken)terminalNode.Symbol).File, terminalNode.Symbol.StartIndex, terminalNode.Symbol.StopIndex - terminalNode.Symbol.StartIndex + 1)
        {
            ParseTree = terminalNode;
        }

        protected HtmlNode(ITerminalNode terminalNode, int start, int length)
            : base(((HtmlMarkdownToken)terminalNode.Symbol).File, start, length)
        {
            ParseTree = terminalNode;
        }
    }
}