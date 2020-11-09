using System;
using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public class MarkdownLeafBlockNode : MarkdownNode
    {
        public LeafBlock LeafBlock { get; }

        public Node Inline { get; }

        public MarkdownLeafBlockNode(LeafBlock leafBlock, Node inline)
        {
            LeafBlock = leafBlock;
            Inline = inline;
        }
    }
}