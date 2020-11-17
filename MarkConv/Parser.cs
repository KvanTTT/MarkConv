using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Html;
using MarkConv.Html;
using MarkConv.Links;
using MarkConv.Nodes;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class Parser
    {
        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        private TextFile _file;

        private Dictionary<Node, Link> _links;
        private Dictionary<string, Anchor> _anchors;
        private HeaderToLinkConverter _headerToLinkConverter;
        private string _endOfLine;

        public Parser(ProcessorOptions options, ILogger logger)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
        }

        public ParseResult Parse(TextFile file)
        {
            var builder = new MarkdownPipelineBuilder {PreciseSourceLocation = true};
            builder.UseAutoLinks();
            _file = file;
            _links = new Dictionary<Node, Link>();
            _anchors = new Dictionary<string, Anchor>();
            _headerToLinkConverter = new HeaderToLinkConverter(_anchors);
            MarkdownDocument document = Markdown.Parse(_file.Data, builder.Build());
            _endOfLine = GetEndOfLine();
            return new ParseResult(_file, new MarkdownContainerBlockNode(document, Parse(document), _file),
                _links, _anchors, _endOfLine);
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

        private List<Node> Parse(ContainerBlock containerBlock)
        {
            List<Node> children;
            if (containerBlock.Any(child => child is HtmlBlock))
            {
                children = Parse(containerBlock.Cast<MarkdownObject>().ToList());
            }
            else
            {
                children = new List<Node>(containerBlock.Count);
                foreach (Block child in containerBlock)
                    children.Add(ParseMarkdownBlock(child));
            }

            return children;
        }

        private List<Node> Parse(List<MarkdownObject> markdownObjects)
        {
            var tokens = new List<IToken>(markdownObjects.Count);

            var errorListener = new AntlrErrorListener(Logger);

            foreach (MarkdownObject markdownObject in markdownObjects)
            {
                var blockSpan = markdownObject.Span;
                if (markdownObject is HtmlBlock || markdownObject is HtmlInline)
                {
                    var lexer = new HtmlLexer(new AntlrInputStream(_file.GetSubstring(blockSpan.Start, blockSpan.Length)));
                    lexer.AddErrorListener(errorListener);
                    var currentTokens = lexer.GetAllTokens();

                    foreach (IToken token in currentTokens)
                        tokens.Add(new HtmlToken(_file, token.Type, tokens.Count,
                            token.StartIndex + blockSpan.Start, token.StopIndex + blockSpan.Start, token.Text, token.Channel));
                }
                else
                {
                    tokens.Add(new MarkdownToken(_file, tokens.Count, blockSpan.Start, blockSpan.End,
                        ParseMarkdown(markdownObject)));
                }
            }

            var parser = new HtmlParser(new CommonTokenStream(new ListTokenSource(tokens)), Logger);
            parser.AddErrorListener(errorListener);
            var root = parser.root();

            var children = new List<Node>(root.content().Length);
            foreach (var contentContext in root.content())
                children.Add(ParseContent(contentContext));

            return children;
        }

        private Node ParseContent(HtmlParser.ContentContext contentContext)
        {
            if (contentContext.element() != null)
                return ParseElementNode(contentContext.element());

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

        private HtmlElementNode ParseElementNode(HtmlParser.ElementContext elementContext)
        {
            var content = new List<Node>(elementContext.content().Length);
            foreach (var contentContext in elementContext.content())
                content.Add(ParseContent(contentContext));

            bool voidElement = elementContext.voidElementTag() != null;

            var tagName = new HtmlStringNode(voidElement
                ? (ITerminalNode) elementContext.voidElementTag().GetChild(0)
                : elementContext.TAG_NAME(0));
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
            string tagNameString = tagName.String;
            string addressAttrName = null;

            if (tagNameString == "a")
            {
                addressAttrName = "href";
            }
            else if (tagNameString == "img")
            {
                addressAttrName = "src";
                isImage = true;
            }

            if (addressAttrName != null)
            {
                if (attributes.TryGetValue(addressAttrName, out HtmlAttributeNode htmlAttributeNode))
                    address = htmlAttributeNode.Value;
                else
                    Logger.Warn($"Element <{tagNameString}> does not contain required '{addressAttrName}' attribute at {tagName.LineColumnSpan}");
            }

            var selfClosingTagSymbol = voidElement
                ? (ITerminalNode)elementContext.GetChild(elementContext.ChildCount - 1)
                : elementContext.TAG_SLASH_CLOSE();
            var result = new HtmlElementNode(elementContext, tagName, attributes, content,
                selfClosingTagSymbol == null ? null : new HtmlStringNode(selfClosingTagSymbol));

            if (address != null)
                _links.Add(result, Link.Create(result, address.String, isImage, address.Start, address.Length));

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
                    throw new InvalidProgramException($"Parsing of {nameof(HtmlBlock)} should be implemented in {nameof(Parse)}");

                case LeafBlock leafBlock:
                    var inlineNode = ParseMarkdownInline(leafBlock.Inline);
                    var result = new MarkdownLeafBlockNode(leafBlock, inlineNode, _file);

                    if (leafBlock is HeadingBlock)
                        _headerToLinkConverter.Convert(result, Options.InputMarkdownType);

                    return result;

                case ContainerBlock containerBlock:
                    return new MarkdownContainerBlockNode(containerBlock, Parse(containerBlock), _file);

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
                    throw new InvalidProgramException($"Parsing of {nameof(HtmlInline)} should be implemented in {nameof(Parse)}");

                case LiteralInline literalInline:
                    return new MarkdownLeafInlineNode(literalInline, _file);

                case AutolinkInline autolinkInline:
                    result = new MarkdownLeafInlineNode(autolinkInline, _file);
                    var span = autolinkInline.Span;
                    _links.Add(result, Link.Create(result, autolinkInline.Url, start: span.Start + 1, length: span.Length - 2));
                    return result;

                case LeafInline leafInline:
                    return new MarkdownLeafInlineNode(leafInline, _file);

                case ContainerInline containerInline:
                    List<Node> children = containerInline.Any(child => child is HtmlInline)
                        ? Parse(containerInline.Cast<MarkdownObject>().ToList())
                        : containerInline.Select(ParseMarkdownInline).Cast<Node>().ToList();

                    int start = -1, length = -1;
                    if (children.Count > 0 && containerInline.Span.Length == 1)
                    {
                        start = children[0].Start;
                        var last = children[^1];
                        length = last.Start + last.Length - start;
                    }

                    result = new MarkdownContainerInlineNode(containerInline, children, _file, start, length);

                    if (containerInline is LinkInline linkInline)
                    {
                        var urlSpan = linkInline.UrlSpan.Value;
                        _links.Add(result, Link.Create(result, linkInline.Url, linkInline.IsImage, urlSpan.Start, urlSpan.Length));
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