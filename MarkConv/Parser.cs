using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly ProcessorOptions _options;
        private readonly ILogger _logger;
        private readonly TextFile _file;
        private readonly Dictionary<Node, Link> _links;
        private readonly Dictionary<Link, Link> _linksMap;
        private readonly Dictionary<string, Anchor> _anchors;
        private readonly HeaderToLinkConverter _headerToLinkConverter;
        private readonly string _endOfLine;
        private readonly MarkdownDocument _markdownDocument;

        private Link? _headerImageLink;

        public const string LinkmapTagName = "linkmap";
        public const string IncludeTagName = "include";
        public const string HeaderImageLink = "HeaderImageLink";

        public Parser(ProcessorOptions options, ILogger logger, TextFile file)
        {
            _options = options;
            _logger = logger;
            var builder = new MarkdownPipelineBuilder {PreciseSourceLocation = true};
            builder.UseAutoLinks();
            builder.UseGridTables().UsePipeTables();
            _file = file;
            _links = new Dictionary<Node, Link>();
            _linksMap = new Dictionary<Link, Link>(LinkAddressComparer.Instance);
            _anchors = new Dictionary<string, Anchor>();
            _headerToLinkConverter = new HeaderToLinkConverter(_anchors);
            _markdownDocument = Markdown.Parse(_file.Data, builder.Build());
            _endOfLine = GetEndOfLine();
        }

        public ParseResult Parse() =>
            new ParseResult(_file, new MarkdownContainerBlockNode(_markdownDocument, Parse(_markdownDocument), _file),
                _links, _linksMap, _headerImageLink, _anchors, _endOfLine);

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

            var errorListener = new AntlrErrorListener(_logger);

            foreach (MarkdownObject markdownObject in markdownObjects)
            {
                var blockSpan = markdownObject.Span;
                if (markdownObject is HtmlBlock || markdownObject is HtmlInline)
                {
                    var lexer = new HtmlLexer(new CaseInsensitiveInputStream(_file.GetSubstring(blockSpan.Start, blockSpan.Length)));
                    lexer.AddErrorListener(errorListener);
                    var currentTokens = lexer.GetAllTokens();

                    foreach (IToken token in currentTokens)
                        tokens.Add(new HtmlToken(_file, token.Type, tokens.Count,
                            token.StartIndex + blockSpan.Start, token.StopIndex + blockSpan.Start, token.Text, token.Channel));
                }
                else
                {
                    var markdownNode = ParseMarkdown(markdownObject);
                    if (markdownNode != null)
                        tokens.Add(new MarkdownToken(_file, tokens.Count, blockSpan.Start, blockSpan.End, markdownNode));
                }
            }

            var parser = new HtmlParser(new CommonTokenStream(new ListTokenSource(tokens)), _logger);
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
                : elementContext.TAG_NAME());
            var attributes = new Dictionary<string, HtmlAttributeNode>();

            foreach (HtmlParser.AttributeContext attributeContext in elementContext.attribute())
            {
                var nameNode = new HtmlStringNode(attributeContext.TAG_NAME());

                var valueTerminal = attributeContext.ATTR_VALUE();
                var valueSymbol = valueTerminal.Symbol;
                string value = valueSymbol.Text.Trim('\'', '"');
                var valueNode = new HtmlStringNode(valueTerminal, value,
                    valueSymbol.StartIndex, valueSymbol.StopIndex - valueSymbol.StartIndex + 1);

                attributes.Add(nameNode.String.ToLowerInvariant(), new HtmlAttributeNode(attributeContext, nameNode, valueNode));
            }

            HtmlStringNode? address = null;
            bool isImage = false;
            string tagNameString = tagName.String.ToLowerInvariant();
            string? addressAttrName = null;

            if (tagNameString == "a")
            {
                addressAttrName = "href";
            }
            else if (tagNameString == "img")
            {
                addressAttrName = "src";
                isImage = true;
            }
            else if (tagNameString == LinkmapTagName)
            {
                Link? srcLink = null, dstLink = null;

                if (!attributes.TryGetValue("src", out HtmlAttributeNode? srcNode))
                {
                    _logger.Error($"{LinkmapTagName} element should contain src attribute at {tagName.LineColumnSpan}");
                }
                else
                {
                    srcLink = Link.Create(srcNode.Value, srcNode.Value.String);
                    Link? existingLink;
                    if ((existingLink = _linksMap.Keys.FirstOrDefault(key => key.Address.Equals(srcLink.Address))) != null)
                    {
                        _logger.Warn($"{LinkmapTagName} \"{srcLink.Node.Substring}\" at {srcLink.Node.LineColumnSpan} replaces linkmap at {existingLink.Node.LineColumnSpan}");
                    }
                }

                if (!attributes.TryGetValue("dst", out HtmlAttributeNode? dstNode))
                {
                    _logger.Error($"{LinkmapTagName} element should contain dst attribute at {tagName.LineColumnSpan}");
                }
                else
                {
                    dstLink = Link.Create(dstNode.Value, dstNode.Value.String);
                }

                if (srcLink != null && dstLink != null)
                {
                    if (srcLink.Address == HeaderImageLink)
                        _headerImageLink = dstLink;
                    else
                        _linksMap[srcLink] = dstLink;
                }
            }
            else if (tagNameString == IncludeTagName)
            {
                if (!attributes.TryGetValue("src", out HtmlAttributeNode? srcNode))
                {
                    _logger.Error($"{IncludeTagName} element should contain src attribute at {tagName.LineColumnSpan}");
                }
                else
                {
                    var srcLink = Link.Create(srcNode.Value, srcNode.Value.String);
                    if (srcLink is LocalLink localLink)
                    {
                        var rootDirectory = Path.GetDirectoryName(_file.Name) ?? "";
                        var includeFilePath = Path.Combine(rootDirectory, localLink.Address);
                        if (!File.Exists(includeFilePath))
                        {
                            _logger.Error($"File {includeFilePath} does not exist at {localLink.Node.LineColumnSpan}");
                        }
                        else
                        {
                            var includeFile = new TextFile(File.ReadAllText(includeFilePath), includeFilePath);
                            var parser = new Parser(_options, _logger, includeFile);
                            var includeParseResult = parser.Parse();

                            foreach (KeyValuePair<Node, Link> pair in includeParseResult.Links)
                                _links.Add(pair.Key, pair.Value);

                            foreach ((Link includeKey, Link includeValue) in includeParseResult.LinksMap)
                            {
                                Link? existingLinkMap;
                                if ((existingLinkMap = _linksMap.Keys.FirstOrDefault(key => key.Address.Equals(includeKey.Address))) != null)
                                {
                                    _logger.Warn($"{LinkmapTagName} \"{existingLinkMap.Node.Substring}\" at {existingLinkMap.Node.LineColumnSpan} replaces " +
                                                    $"linkmap at {includeKey.Node.LineColumnSpan} at {includeKey.Node.File.Name}");
                                }
                                else
                                {
                                    _linksMap.Add(includeKey, includeValue);
                                }
                            }

                            foreach ((string anchorKey, Anchor anchorValue) in includeParseResult.Anchors)
                            {
                                if (_anchors.TryGetValue(anchorKey, out Anchor? existingAnchor))
                                {
                                    _logger.Warn($"Anchor {existingAnchor.Address} at {existingAnchor.Node.LineColumnSpan} replaces "
                                                + $"anchor at {anchorValue.Node.LineColumnSpan} at {anchorValue.Node.File.Name}");
                                }
                                else
                                {
                                    _anchors.Add(anchorKey, anchorValue);
                                }
                            }

                            content.Add(includeParseResult.Node);
                        }
                    }
                    else
                    {
                        _logger.Error($"Only local files can be included via <include/> element at {srcLink.Node.LineColumnSpan}");
                    }
                }
            }

            if (addressAttrName != null)
            {
                if (attributes.TryGetValue(addressAttrName, out HtmlAttributeNode? htmlAttributeNode))
                    address = htmlAttributeNode.Value;
                else
                    _logger.Error($"Element <{tagNameString}> does not contain required '{addressAttrName}' attribute at {tagName.LineColumnSpan}");
            }

            var closingTag = elementContext.GetChild(elementContext.ChildCount - 1);
            var result = new HtmlElementNode(elementContext, tagName, attributes, content,
                closingTag is ParserRuleContext parserRuleContext
                    ? new HtmlStringNode(parserRuleContext)
                    : new HtmlStringNode((ITerminalNode) closingTag));

            if (address != null)
                _links.Add(result, Link.Create(result, address.String, isImage, address.Start, address.Length));

            return result;
        }

        private MarkdownNode? ParseMarkdown(MarkdownObject markdownObject)
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
                        _headerToLinkConverter.Convert(result, _options.InputMarkdownType);

                    return result;

                case ContainerBlock containerBlock:
                    return new MarkdownContainerBlockNode(containerBlock, Parse(containerBlock), _file);

                default:
                    throw new NotImplementedException($"Converting of Block type '{block.GetType()}' is not implemented");
            }
        }

        private MarkdownNode? ParseMarkdownInline(Inline? inline)
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
                        : containerInline.Select(ParseMarkdownInline).Where(node => node != null).Cast<Node>().ToList();

                    int start = -1, length = -1;
                    if (children.Count > 0 && containerInline.Span.Length == 1)
                    {
                        start = children[0].Start;
                        var last = children[^1];
                        length = last.Start + last.Length - start;
                    }

                    result = new MarkdownContainerInlineNode(containerInline, children, _file, start, length);

                    if (containerInline is LinkInline { UrlSpan: var urlSpan, Url: { } url } linkInline)
                    {
                        _links.Add(result, Link.Create(result, url, linkInline.IsImage, urlSpan.Start, urlSpan.Length));
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