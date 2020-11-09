using System;
using Antlr4.Runtime;

namespace MarkConv.Nodes
{
    public class HtmlCommentNode : HtmlNode
    {
        public IToken CommentToken { get; }

        public HtmlCommentNode(IToken commentToken)
        {
            CommentToken = commentToken ?? throw new ArgumentNullException(nameof(commentToken));
        }
    }
}