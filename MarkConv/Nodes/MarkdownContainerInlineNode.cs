using System;
using System.Collections.Generic;
using Markdig.Syntax.Inlines;

namespace MarkConv.Nodes
{
    public class MarkdownContainerInlineNode : MarkdownNode
    {
        public ContainerInline ContainerInline { get; }

        public List<Node> Children { get; }

        public MarkdownContainerInlineNode(ContainerInline containerInline, List<Node> children)
        {
            ContainerInline = containerInline ?? throw new ArgumentNullException(nameof(containerInline));
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }
}