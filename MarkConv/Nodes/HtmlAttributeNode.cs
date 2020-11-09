using System;

namespace MarkConv.Nodes
{
    public class HtmlAttributeNode : HtmlNode
    {
        public HtmlStringNode Name { get; }

        public HtmlStringNode Value { get; }

        public HtmlAttributeNode(HtmlParser.AttributeContext attributeContext, HtmlStringNode name, HtmlStringNode value)
            : base(attributeContext)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public override string ToString() => $"{Name} = {Value}";
    }
}