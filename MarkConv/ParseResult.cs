using System;
using System.Collections.Generic;
using MarkConv.Links;
using MarkConv.Nodes;

namespace MarkConv
{
    public class ParseResult
    {
        public TextFile File { get; }

        public Node Node { get; }

        public IReadOnlyDictionary<Node, Link> Links { get; }

        public IReadOnlyDictionary<Link, Link> LinksMap { get; }

        public Link? HeaderImageLink { get; }

        public IReadOnlyDictionary<string, Anchor> Anchors { get; }

        public string EndOfLine { get; }

        public ParseResult(TextFile file, Node node, Dictionary<Node, Link> links, Dictionary<Link, Link> linksMap, Link? headerImageLink,
            Dictionary<string, Anchor> anchors, string endOfLine)
        {
            File = file;
            Node = node;
            Links = links;
            LinksMap = linksMap;
            HeaderImageLink = headerImageLink;
            Anchors = anchors;
            EndOfLine = endOfLine;
        }
    }
}