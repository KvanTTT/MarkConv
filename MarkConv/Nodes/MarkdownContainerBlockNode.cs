using System;
using System.Collections.Generic;
using Markdig.Syntax;

namespace MarkConv.Nodes
{
    public class MarkdownContainerBlockNode : MarkdownNode
    {
        public ContainerBlock ContainerBlock { get; }

        public List<Node> Children { get; }

        public MarkdownContainerBlockNode(ContainerBlock containerBlock, List<Node> children)
        {
            ContainerBlock = containerBlock ?? throw new ArgumentNullException(nameof(containerBlock));
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }
}