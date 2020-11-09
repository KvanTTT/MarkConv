using System;
using System.Collections.Generic;
using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public class MarkdownContainerBlockNode : MarkdownNode
    {
        public ContainerBlock ContainerBlock => (ContainerBlock) MarkdownObject;

        public List<Node> Children { get; }

        public MarkdownContainerBlockNode(ContainerBlock containerBlock, List<Node> children)
            : base(containerBlock)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }
}