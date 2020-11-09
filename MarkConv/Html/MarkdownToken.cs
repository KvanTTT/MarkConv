using System;
using MarkConv.Nodes;

namespace MarkConv.Html
{
    public class MarkdownToken : HtmlMarkdownToken
    {
        public MarkdownNode MarkdownNode { get; }

        public override string Text => MarkdownNode.ToString();

        public override int Type => HtmlLexer.MARKDOWN_FRAGMENT;

        public MarkdownToken(int index, int start, int stop, MarkdownNode markdownNode)
            : base(index, start, stop)
        {
            MarkdownNode = markdownNode ?? throw new ArgumentNullException(nameof(markdownNode));
        }
    }
}