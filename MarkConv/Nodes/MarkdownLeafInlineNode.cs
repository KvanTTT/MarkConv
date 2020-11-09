using System;
using Markdig.Syntax.Inlines;

namespace MarkConv.Nodes
{
    public class MarkdownLeafInlineNode : MarkdownNode
    {
        public LeafInline LeafInline { get; }

        public MarkdownLeafInlineNode(LeafInline leafInline) =>
            LeafInline = leafInline ?? throw new ArgumentNullException(nameof(leafInline));
    }
}