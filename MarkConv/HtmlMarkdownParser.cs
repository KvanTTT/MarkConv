using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static readonly Regex UrlRegex = new Regex(
            @"https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,}",
            RegexOptions.Compiled);

        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        private TextFile _file;

        private readonly List<Link> _links = new List<Link>();

        public IReadOnlyList<Link> Links => _links;

        public string EndOfLine { get; private set; }

        public HtmlMarkdownParser(ProcessorOptions options, ILogger logger)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
        }

        public Node ParseHtmlMarkdown(TextFile file)
        {
            var builder = new MarkdownPipelineBuilder { PreciseSourceLocation = true };
            _file = file;
            MarkdownDocument document = Markdown.Parse(_file.Data, builder.Build());
            EndOfLine = GetEndOfLine();
            return new MarkdownContainerBlockNode(document, ParseHtmlMarkdown(document), _file);
        }

        private string GetEndOfLine()
        {
            int crlfCount = 0;
            int lfCount = 0;

            var lineStartIndexes = _file.LineIndexes;
            var data = _file.Data;

            for (var index = 1; index < lineStartIndexes.Length; index++)
            {
                int lineStartIndex = lineStartIndexes[index];
                if (lineStartIndex - 2 > 0 && data[lineStartIndex - 2] == '\r')
                    crlfCount++;
                else
                    lfCount++;
            }

            return crlfCount > lfCount ? "\r\n" : "\n";
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
                if (markdownObject is HtmlBlock || markdownObject is HtmlInline)
                {
                    var lexer = new HtmlLexer(new AntlrInputStream(_file.GetSubstring(blockSpan.Start, blockSpan.Length)));
                    var currentTokens = lexer.GetAllTokens();

                    foreach (IToken token in currentTokens)
                        tokens.Add(new HtmlToken(_file, token.Type, tokens.Count,
                            token.StartIndex + blockSpan.Start, token.StopIndex + blockSpan.Start, token.Text));
                }
                else
                {
                    tokens.Add(new MarkdownToken(_file, tokens.Count, blockSpan.Start, blockSpan.End,
                        ParseMarkdown(markdownObject)));
                }
            }

            var parser = new HtmlParser(new CommonTokenStream(new ListTokenSource(tokens)));
            var root = parser.root();

            var children = new List<Node>(root.content().Length);
            foreach (var contentContext in root.content())
                children.Add(ProcessContent(contentContext));

            return children;
        }

        private Node ProcessContent(HtmlParser.ContentContext contentContext)
        {
            if (contentContext.element() != null)
                return ProcessElementNode(contentContext.element());

            if (contentContext.HTML_COMMENT() != null)
            {
                var commentTerminal = contentContext.HTML_COMMENT();
                var commentSymbol = commentTerminal.Symbol;
                var comment = commentSymbol.Text;
                comment = comment.Remove(comment.Length - 3).Remove(0, 4); // Unescape comment
                return new HtmlCommentNode(commentTerminal, comment,
                    commentSymbol.StartIndex, commentSymbol.StopIndex - commentSymbol.StartIndex + 1);
            }

            if (contentContext.HTML_TEXT() != null)
                return new HtmlStringNode(contentContext.HTML_TEXT());

            var markdownToken = (MarkdownToken) contentContext.MARKDOWN_FRAGMENT().Symbol;
            return markdownToken.MarkdownNode;
        }

        private HtmlElementNode ProcessElementNode(HtmlParser.ElementContext elementContext)
        {
            var content = new List<Node>(elementContext.content().Length);
            foreach (var contentContext in elementContext.content())
                content.Add(ProcessContent(contentContext));

            var tagName = new HtmlStringNode(elementContext.TAG_NAME(0));
            var attributes = new Dictionary<string, HtmlAttributeNode>();

            foreach (HtmlParser.AttributeContext attributeContext in elementContext.attribute())
            {
                var nameNode = new HtmlStringNode(attributeContext.TAG_NAME());

                var valueTerminal = attributeContext.ATTR_VALUE();
                var valueSymbol = valueTerminal.Symbol;
                string value = valueSymbol.Text.Trim('\'', '"');
                var valueNode = new HtmlStringNode(valueTerminal, value,
                    valueSymbol.StartIndex, valueSymbol.StopIndex - valueSymbol.StartIndex + 1);

                attributes.Add(nameNode.String, new HtmlAttributeNode(attributeContext, nameNode, valueNode));
            }

            HtmlStringNode address = null;
            bool isImage = false;

            // TODO: should check if such attributes presented and throw an error if not
            if (tagName.String == "a")
            {
                address = attributes["href"].Value;
            }
            else if (tagName.String == "img")
            {
                address = attributes["src"].Value;
                isImage = true;
            }

            var selfClosingTagSymbol = elementContext.TAG_SLASH_CLOSE();
            var result = new HtmlElementNode(elementContext, tagName, attributes, content,
                selfClosingTagSymbol == null ? null : new HtmlStringNode(selfClosingTagSymbol));

            if (address != null)
                _links.Add(new Link(result, address.String, isImage, start: address.Start, length: address.Length));

            return result;
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
                    return new MarkdownLeafBlockNode(leafBlock, ParseMarkdownInline(leafBlock.Inline), _file);

                case ContainerBlock containerBlock:
                    return new MarkdownContainerBlockNode(containerBlock, ParseHtmlMarkdown(containerBlock), _file);

                default:
                    throw new NotImplementedException($"Converting of Block type '{block.GetType()}' is not implemented");
            }
        }

        private MarkdownNode ParseMarkdownInline(Inline inline)
        {
            MarkdownNode result;

            switch (inline)
            {
                case HtmlInline _:
                    throw new InvalidProgramException($"Parsing of {nameof(HtmlInline)} should be implemented in {nameof(ParseHtmlMarkdown)}");

                case LiteralInline literalInline:
                    result = new MarkdownLeafInlineNode(literalInline, _file);
                    var offset = literalInline.Span.Start;
                    var matches = UrlRegex.Matches(literalInline.Content.ToString());
                    foreach (Match url in matches)
                        _links.Add(new Link(result, url.Value, start: offset + url.Index, length: url.Length));
                    return result;

                case AutolinkInline autolinkInline:
                    result = new MarkdownLeafInlineNode(autolinkInline, _file);
                    var span = autolinkInline.Span;
                    _links.Add(new Link(result, autolinkInline.Url, start: span.Start + 1, length: span.Length - 2));
                    return result;

                case LeafInline leafInline:
                    return new MarkdownLeafInlineNode(leafInline, _file);

                case ContainerInline containerInline:
                    List<Node> children = containerInline.Any(child => child is HtmlInline)
                        ? ParseHtmlMarkdown(containerInline.Cast<MarkdownObject>().ToList())
                        : containerInline.Select(ParseMarkdownInline).Cast<Node>().ToList();
                    result = new MarkdownContainerInlineNode(containerInline, children, _file);

                    if (containerInline is LinkInline linkInline)
                    {
                        var urlSpan = linkInline.UrlSpan.Value;
                        _links.Add(new Link(result, linkInline.Url, linkInline.IsImage, start: urlSpan.Start, length: urlSpan.Length));
                    }

                    return result;

                case null:
                    return null;

                default:
                    throw new NotImplementedException($"Parsing of Inline type '{inline.GetType()}' is not implemented");
            }
        }
    }
}