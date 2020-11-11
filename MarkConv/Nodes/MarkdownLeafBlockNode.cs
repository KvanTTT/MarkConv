using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public class MarkdownLeafBlockNode : MarkdownNode
    {
        public LeafBlock LeafBlock => (LeafBlock) MarkdownObject;

        public MarkdownNode Inline { get; }

        public MarkdownLeafBlockNode(LeafBlock leafBlock, MarkdownNode inline, TextFile file)
            : base(leafBlock, file)
        {
            Inline = inline;
        }
    }
}