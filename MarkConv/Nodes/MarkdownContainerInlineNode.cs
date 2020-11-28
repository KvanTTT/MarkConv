using System;
using System.Collections.Generic;
using Markdig.Syntax.Inlines;

namespace MarkConv.Nodes
{
    public class MarkdownContainerInlineNode : MarkdownNode
    {
        public ContainerInline ContainerInline => (ContainerInline) MarkdownObject;

        public List<Node> Children { get; }

        public MarkdownContainerInlineNode(ContainerInline containerInline, List<Node> children, TextFile file,
            int start = -1, int length = -1)
            : base(containerInline, file, start, length)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }
}