using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public abstract class MarkdownNode : Node
    {
        public MarkdownObject MarkdownObject { get; }

        protected MarkdownNode(MarkdownObject markdownObject) : base(markdownObject.Span.Start, markdownObject.Span.Length)
        {
            MarkdownObject = markdownObject;
        }
    }
}