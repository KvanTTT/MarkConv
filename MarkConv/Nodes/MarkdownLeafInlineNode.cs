using Markdig.Syntax.Inlines;

namespace MarkConv.Nodes
{
    public class MarkdownLeafInlineNode : MarkdownNode
    {
        public LeafInline LeafInline => (LeafInline) MarkdownObject;

        public MarkdownLeafInlineNode(LeafInline leafInline) : base(leafInline) {}
    }
}