using System;
using Antlr4.Runtime;

namespace MarkConv.Nodes
{
    public class HtmlTextNode : HtmlNode
    {
        public IToken TextToken { get; }

        public HtmlTextNode(IToken textToken)
        {
            TextToken = textToken ?? throw new ArgumentNullException(nameof(textToken));
        }
    }
}