using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using MarkConv.Html;
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

        private List<Link> _links = new List<Link>();

        public IReadOnlyList<Link> Links => _links;

        public HtmlMarkdownParser(ProcessorOptions options, ILogger logger)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
        }

        public Node ParseHtmlMarkdown(string content)
        {
            MarkdownDocument document = Markdown.Parse(content);
            return new MarkdownContainerBlockNode(document, ParseHtmlMarkdown(document));
        }

        private List<Node> ParseHtmlMarkdown(ContainerBlock containerBlock)
        {
            List<Node> children;
            if (containerBlock.Any(child => child is HtmlBlock))
            {
                children = ParseHtmlMarkdown(containerBlock.Cast<MarkdownObject>().ToList());
            }
            else
            {
                children = new List<Node>(containerBlock.Count);
                foreach (Block child in containerBlock)
                    children.Add(ParseMarkdownBlock(child));
            }

            return children;
        }

        private List<Node> ParseHtmlMarkdown(List<MarkdownObject> markdownObjects)
        {
            var tokens = new List<IToken>(markdownObjects.Count);

            foreach (MarkdownObject markdownObject in markdownObjects)
            {
                var blockSpan = markdownObject.Span;
                if (markdownObject is HtmlBlock htmlBlock)
                {
                    var builder = new StringBuilder();

                    ReadOnlySpan<char> origSpan = default;
                    var lines = htmlBlock.Lines.Lines;
                    for (var index = 0; index < htmlBlock.Lines.Count; index++)
                    {
                        var line = lines[index];
                        var slice = line.Slice;
                        if (origSpan == default)
                            origSpan = slice.Text.AsSpan();

                        builder.Append(origSpan.Slice(slice.Start, slice.Length));
                        builder.Append('\n');
                    }

                    TokenizeAndAppend(tokens, builder.ToString(), blockSpan.Start);
                }
                else if (markdownObject is HtmlInline htmlInline)
                {
                    TokenizeAndAppend(tokens, htmlInline.Tag, blockSpan.Start);
                }
                else
                {
                    tokens.Add(new MarkdownToken(tokens.Count, blockSpan.Start, blockSpan.End, ParseMarkdown(markdownObject)));
                }
            }

            var parser = new HtmlParser(new CommonTokenStream(new ListTokenSource(tokens)));
            var root = parser.root();

            var children = new List<Node>(root.content().Length);
            foreach (var contentContext in root.content())
                children.Add(ProcessContent(contentContext));

            return children;
        }

        private static void TokenizeAndAppend(List<IToken> tokens, string input, int offset)
        {
            var lexer = new HtmlLexer(new AntlrInputStream(input));
            var currentTokens = lexer.GetAllTokens();

            foreach (IToken token in currentTokens)
            {
                tokens.Add(new HtmlToken(token.Type, tokens.Count,
                    token.StartIndex + offset, token.StopIndex + offset, token.Text));
            }
        }

        private Node ProcessContent(HtmlParser.ContentContext contentContext)
        {
            if (contentContext.element() != null)
            {
                return ProcessElementNode(contentContext.element());
            }

            if (contentContext.HTML_COMMENT() != null)
            {
                return new HtmlCommentNode(contentContext.HTML_COMMENT().Symbol);
            }

            if (contentContext.HTML_TEXT() != null)
            {
                return new HtmlTextNode(contentContext.HTML_TEXT().Symbol);
            }

            var markdownToken = (MarkdownToken) contentContext.MARKDOWN_FRAGMENT().Symbol;
            return markdownToken.MarkdownNode;
        }

        private HtmlElementNode ProcessElementNode(HtmlParser.ElementContext elementContext)
        {
            var content = new List<Node>(elementContext.content().Length);
            foreach (var contentContext in elementContext.content())
                content.Add(ProcessContent(contentContext));

            return new HtmlElementNode(elementContext.TAG_NAME(0).Symbol,
                elementContext.attribute(), content, elementContext.TAG_SLASH_CLOSE()?.Symbol);
        }

        private MarkdownNode ParseMarkdown(MarkdownObject markdownObject)
        {
            if (markdownObject is Block block)
                return ParseMarkdownBlock(block);

            if (markdownObject is Inline inline)
                return ParseMarkdownInline(inline);

            throw new NotImplementedException($"Converting of type '{markdownObject.GetType()}' is not implemented");
        }

        private MarkdownNode ParseMarkdownBlock(Block block)
        {
            switch (block)
            {
                case HtmlBlock _:
                    throw new InvalidProgramException($"Parsing of {nameof(HtmlBlock)} should be implemented in {nameof(ParseHtmlMarkdown)}");

                case LeafBlock leafBlock:
                    return new MarkdownLeafBlockNode(leafBlock, ParseMarkdownInline(leafBlock.Inline));

                case ContainerBlock containerBlock:
                    return new MarkdownContainerBlockNode(containerBlock, ParseHtmlMarkdown(containerBlock));

                default:
                    throw new NotImplementedException($"Converting of Block type '{block.GetType()}' is not implemented");
            }
        }

        private MarkdownNode ParseMarkdownInline(Inline inline)
        {
            switch (inline)
            {
                case AutolinkInline autolinkInline:
                    _links.Add(new Link(autolinkInline.Url, autolinkInline.Url));
                    return new MarkdownLeafInlineNode(autolinkInline);

                case HtmlInline _:
                    throw new InvalidProgramException($"Parsing of {nameof(HtmlInline)} should be implemented in {nameof(ParseHtmlMarkdown)}");

                case LeafInline leafInline:
                    return new MarkdownLeafInlineNode(leafInline);

                case ContainerInline containerInline:
                    if (containerInline is LinkInline linkInline)
                    {
                        string title = linkInline.Title;
                        if (string.IsNullOrEmpty(title))
                            title = linkInline.FirstChild.ToString();
                        _links.Add(new Link(linkInline.Url, title, linkInline.IsImage));
                    }

                    List<Node> children;
                    if (containerInline.Any(child => child is HtmlInline))
                    {
                        children = ParseHtmlMarkdown(containerInline.Cast<MarkdownObject>().ToList());
                    }
                    else
                    {
                        children = new List<Node>(containerInline.Count());
                        foreach (Inline inline2 in containerInline)
                            children.Add(ParseMarkdownInline(inline2));
                    }
                    return new MarkdownContainerInlineNode(containerInline, children);

                case null:
                    return null;

                default:
                    throw new NotImplementedException($"Parsing of Inline type '{inline.GetType()}' is not implemented");
            }
        }
    }
}