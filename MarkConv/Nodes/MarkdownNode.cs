using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public abstract class MarkdownNode : Node
    {
        public MarkdownObject MarkdownObject { get; }

        protected MarkdownNode(MarkdownObject markdownObject, TextFile file, int start = -1, int length = -1)
            : base(file, start == -1 ? markdownObject.Span.Start : start, length == -1 ? markdownObject.Span.Length : length)
        {
            MarkdownObject = markdownObject;
        }
    }
}