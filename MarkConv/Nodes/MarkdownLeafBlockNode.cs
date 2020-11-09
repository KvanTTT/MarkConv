using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public class MarkdownLeafBlockNode : MarkdownNode
    {
        public LeafBlock LeafBlock => (LeafBlock) MarkdownObject;

        public Node Inline { get; }

        public MarkdownLeafBlockNode(LeafBlock leafBlock, Node inline)
            : base(leafBlock)
        {
            Inline = inline;
        }
    }
}