using System.Collections.Generic;

namespace MarkConv.Nodes
{
    public class HtmlElementNode : HtmlNode
    {
        public HtmlStringNode Name { get; }

        public Dictionary<string, HtmlAttributeNode> Attributes { get; }

        public List<Node> Content { get; }

        public HtmlStringNode SelfClosingTag { get; }

        public HtmlElementNode(HtmlParser.ElementContext elementContext, HtmlStringNode name, Dictionary<string, HtmlAttributeNode> attributes, List<Node> content,
            HtmlStringNode selfClosingTag)
            : base(elementContext)
        {
            Name = name;
            Attributes = attributes;
            Content = content;
            SelfClosingTag = selfClosingTag;
        }
    }
}