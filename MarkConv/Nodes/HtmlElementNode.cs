using System.Collections.Generic;
using Antlr4.Runtime;

namespace MarkConv.Nodes
{
    public class HtmlElementNode : HtmlNode
    {
        public IToken Name { get; }

        public HtmlParser.AttributeContext[] Attributes { get; }

        public List<Node> Content { get; }

        public IToken SelfClosingTag { get; }

        public HtmlElementNode(IToken name, HtmlParser.AttributeContext[] attributes, List<Node> content, IToken selfClosingTag)
        {
            Name = name;
            Attributes = attributes;
            Content = content;
            SelfClosingTag = selfClosingTag;
        }
    }
}