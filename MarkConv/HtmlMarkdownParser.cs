using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MarkConv.Nodes;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class HtmlMarkdownParser
    {
        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        private const string MarkdownBlockMarker = "markdown_block:";

        private static readonly Regex MarkdownBlockRegex =
            new Regex(MarkdownBlockMarker + @"(\d+);", RegexOptions.Compiled);

        public HtmlMarkdownParser(ProcessorOptions options, ILogger logger)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
        }

        public Node ParseHtmlMarkdown(string content)
        {
            MarkdownDocument document = Markdown.Parse(content);
            return ParseMarkdownHtml(document, 0);
        }

        private Node ParseMarkdownHtml(ContainerBlock rootContainer, int offset)
        {
            if (rootContainer.All(block => !(block is HtmlBlock)))
            {
                return ParseMarkdown(rootContainer, rootContainer, offset);
            }

            var htmlData = new StringBuilder();

            for (var index = 0; index < rootContainer.Count; index++)
            {
                Block child = rootContainer[index];
                if (child is HtmlBlock htmlBlock)
                {
                    AppendHtmlData(htmlData, htmlBlock);
                }
                else
                {
                    htmlData.Append(MarkdownBlockMarker);
                    htmlData.Append(index);
                    htmlData.Append(';');
                }
            }

            return ParseHtml(htmlData.ToString(), rootContainer, offset);
        }

        private Node ParseHtml(string htmlData, ContainerBlock rootContainer, int offset)
        {
            var doc = new HtmlDocument();
            using var stringReader = new StringReader(htmlData);
            doc.Load(stringReader);
            return ParseHtml(doc.DocumentNode, rootContainer, offset);
        }

        private Node ParseHtml(HtmlNode htmlNode, ContainerBlock rootContainer, int offset)
        {
            if (htmlNode is HtmlTextNode htmlTextNode)
                return ParseHtmlTextNode(htmlTextNode, rootContainer, offset);

            var children = new List<Node>(htmlNode.ChildNodes.Count);
            foreach (HtmlNode childNode in htmlNode.ChildNodes)
                children.Add(ParseHtml(childNode, rootContainer, offset));

            var span = rootContainer.Span;
            return new HtmlMarkdownNode(htmlNode, new HtmlMarkdownNode(htmlNode.EndNode, null, new List<Node>(), 0, 0), children,
                span.Start, span.Length);
        }

        private Node ParseHtmlTextNode(HtmlTextNode htmlTextNode, ContainerBlock rootContainer, int offset)
        {
            Match match;
            int prevIndex = 0;
            int restLength = htmlTextNode.Text.Length;
            var textSpan = htmlTextNode.Text.AsSpan();
            var children = new List<Node>();
            Block markdownBlock = null;

            while ((match = MarkdownBlockRegex.Match(htmlTextNode.Text, prevIndex, restLength)).Success)
            {
                AddHtmlTextNodeIfNotEmpty(prevIndex, match.Index - prevIndex, textSpan);

                int blockNumber = int.Parse(match.Groups[1].Value);
                markdownBlock = rootContainer[blockNumber];

                children.Add(ParseMarkdown(markdownBlock, rootContainer, offset));

                prevIndex = match.Index + match.Length;
                restLength = htmlTextNode.Text.Length - prevIndex;
            }

            AddHtmlTextNodeIfNotEmpty(match.Index, restLength - match.Index, textSpan);

            if (children.Count == 1)
                return children[0];

            return new HtmlMarkdownNode(htmlTextNode.OwnerDocument.CreateElement("#artificial"), null, children,
                rootContainer.Span.Start, rootContainer.Span.Length);

            void AddHtmlTextNodeIfNotEmpty(int index, int length, ReadOnlySpan<char> htmlTextSpan)
            {
                var span = htmlTextSpan.Slice(index, length);
                if (!span.IsEmpty)
                {
                    children.Add(new HtmlMarkdownNode(
                        htmlTextNode.OwnerDocument.CreateTextNode(span.ToString()),
                        offset + (markdownBlock ?? rootContainer).Span.End + 1, span.Length));
                }
            }
        }

        private void AppendHtmlData(StringBuilder htmlData, HtmlBlock htmlBlock)
        {
            ReadOnlySpan<char> origSpan = default;
            var lines = htmlBlock.Lines.Lines;
            for (var index = 0; index < htmlBlock.Lines.Count; index++)
            {
                var line = lines[index];
                var slice = line.Slice;
                if (origSpan == default)
                {
                    origSpan = slice.Text.AsSpan();
                }
                htmlData.Append(origSpan.Slice(slice.Start, slice.Length).TrimStart());
                htmlData.Append('\n');
            }
        }

        private MarkdownNode ParseMarkdown(Block block, ContainerBlock container, int offset)
        {
            List<Node> children;

            switch (block)
            {
                case LeafBlock leafBlock:
                    children = new List<Node>();
                    if (leafBlock.Inline != null)
                        children.Add(ParseInlineMarkdown(leafBlock.Inline, container, offset));
                    break;

                case ContainerBlock containerBlock:
                    children = new List<Node>(containerBlock.Count);
                    foreach (Block child in containerBlock)
                    {
                        var converterNode = child is ContainerBlock containerBlock2
                            ? ParseMarkdownHtml(containerBlock2, offset)
                            : ParseMarkdown(child, container, offset);
                        children.Add(converterNode is MarkdownNode
                            ? converterNode
                            : new MarkdownNode(child, new List<Node>(1) {converterNode}, 0, 0));
                    }
                    break;

                default:
                    throw new NotImplementedException($"Converting of Block type '{block.GetType()}' is not implemented");
            }

            var span = block.Span;
            return new MarkdownNode(block, children, span.Start + offset, span.Length);
        }

        private MarkdownNode ParseInlineMarkdown(Inline inline, ContainerBlock rootContainer, int offset)
        {
            List<Node> children;

            switch (inline)
            {
                case LiteralInline _:
                case LineBreakInline _:
                case CodeInline _:
                case AutolinkInline _:
                    children = new List<Node>(0);
                    break;

                case ContainerInline containerInline:
                    children = new List<Node>(containerInline.Count());
                    foreach (Inline inline2 in containerInline)
                        children.Add(ParseInlineMarkdown(inline2, rootContainer, offset));
                    break;

                case HtmlInline htmlInline:
                    children = new List<Node>(1) {ParseHtml(htmlInline.Tag, rootContainer, offset)};
                    break;

                default:
                    throw new NotImplementedException($"Converting of Inline type '{inline.GetType()}' is not implemented");
            }

            var span = inline.Span;
            return new MarkdownNode(inline, children, span.Start + offset, span.Length);
        }
    }
}