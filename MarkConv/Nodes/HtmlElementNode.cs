﻿using System.Collections.Generic;
using Html;

namespace MarkConv.Nodes
{
    public class HtmlElementNode : HtmlNode
    {
        public HtmlStringNode Name { get; }

        public Dictionary<string, HtmlAttributeNode> Attributes { get; }

        public List<Node> Content { get; }

        public HtmlStringNode SelfClosingTag { get; }

        public bool TryGetChild(string key, out Node child)
        {
            foreach (Node node in Content)
            {
                if (node is HtmlElementNode childHtmlElementNode && childHtmlElementNode.Name.String == key)
                {
                    child = node;
                    return true;
                }
            }

            child = null;
            return false;
        }

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