using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarkConv.Links;
using MarkConv.Nodes;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class Converter
    {
        private ILogger Logger { get; }

        private ProcessorOptions Options { get; }

        private bool _notBreak;
        private bool _lastBlockIsMarkdown;
        private readonly bool _inline;
        private int _headingNumber = -1;

        private ParseResult _parseResult;
        private ConversionResult _result;
        private Dictionary<string, Anchor> _newAnchors;
        private HeaderToLinkConverter _headerToLinkConverter;

        public Converter(ProcessorOptions options, ILogger logger, bool inline = false)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
            _inline = inline;
        }

        public string ConvertAndReturn(ParseResult parseResult)
        {
            _parseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
            _result = new ConversionResult(parseResult.EndOfLine);
            _newAnchors = new Dictionary<string, Anchor>();
            _headerToLinkConverter = new HeaderToLinkConverter(_newAnchors);
            Convert(parseResult.Node, false);
            return _result.ToString();
        }

        private void Convert(Node node, bool ensureNewLine)
        {
            if (node is HtmlNode htmlNode)
            {
                ConvertHtmlNode(htmlNode, ensureNewLine);
                _lastBlockIsMarkdown = false;
            }
            else if (node is MarkdownNode markdownNode)
            {
                ConvertMarkdown(markdownNode, ensureNewLine);
                _lastBlockIsMarkdown = true;
            }
        }

        private void ConvertHtmlNode(HtmlNode htmlNode, bool ensureNewLine)
        {
            switch (htmlNode)
            {
                case HtmlStringNode htmlStringNode:
                    ConvertHtmlTextNode(htmlStringNode);
                    break;

                case HtmlCommentNode htmlCommentNode:
                    ConvertHtmlComment(htmlCommentNode);
                    break;

                case HtmlElementNode htmlElementNode:
                    if (ConvertDetailsOrSpoilerElement(htmlElementNode))
                        return;

                    if (ConvertSummaryElement(htmlElementNode))
                        return;

                    ConvertHtmlElement(htmlElementNode);
                    break;

                default:
                    throw new NotImplementedException($"Conversion of {htmlNode.GetType()} is not implemented");
            }
        }

        private void ConvertHtmlTextNode(HtmlStringNode htmlStringNode)
        {
            _result.Append(htmlStringNode.String.TrimEnd());
        }

        private void ConvertHtmlComment(HtmlCommentNode htmlCommentNode)
        {
            if (!Options.RemoveComments)
            {
                EnsureNewLineIfNotInline();
                _result.Append("<!--");
                _result.Append(htmlCommentNode.Comment);
                _result.Append("-->");
            }
        }

        private bool ConvertDetailsOrSpoilerElement(HtmlElementNode htmlElementNode)
        {
            string name = htmlElementNode.Name.String;
            string detailsTitle = null;
            bool removeDetails = false;
            bool convertDetails = false;

            if (Options.RemoveDetails || Options.InputMarkdownType == MarkdownType.GitHub && Options.OutputMarkdownType != MarkdownType.GitHub)
            {
                if (name == "details")
                {
                    if (htmlElementNode.TryGetChild("summary", out Node child))
                        detailsTitle = (child as HtmlElementNode)?.Content.FirstOrDefault()?.Substring;
                    AssignRemoveOrConvert();
                }
            }
            else if (Options.RemoveDetails || Options.InputMarkdownType == MarkdownType.Habr && Options.OutputMarkdownType != MarkdownType.Habr)
            {
                if (name == "spoiler")
                {
                    if (htmlElementNode.TryGetChild("title", out Node child))
                        detailsTitle = (child as HtmlElementNode)?.Content.FirstOrDefault()?.Substring;
                    AssignRemoveOrConvert();
                }
            }

            void AssignRemoveOrConvert()
            {
                if (Options.RemoveDetails)
                    removeDetails = true;
                else
                    convertDetails = true;
            }

            if (removeDetails)
                return true;

            if (!convertDetails)
                return false;

            EnsureNewLineIfNotInline();

            if (Options.OutputMarkdownType == MarkdownType.GitHub)
            {
                _result.Append("<details>");
                _result.AppendNewLine();

                if (detailsTitle != null)
                {
                    _result.Append("<summary>");
                    _result.Append(detailsTitle);
                    _result.Append("</summary>");
                }

                ConvertChildren(htmlElementNode);

                _result.Append("</details>");
            }
            else if (Options.OutputMarkdownType == MarkdownType.Habr)
            {
                _result.Append("<spoiler");
                if (detailsTitle != null)
                    _result.Append($" title=\"{detailsTitle}\"");
                _result.Append('>');

                ConvertChildren(htmlElementNode);

                _result.Append("</spoiler>");
            }
            else if (Options.OutputMarkdownType == MarkdownType.Dev)
            {
                _result.Append("{% details");
                if (detailsTitle != null)
                {
                    _result.Append(' ');
                    _result.Append(detailsTitle);
                }

                _result.Append(" %}");

                ConvertChildren(htmlElementNode);

                _result.Append("{% enddetails %}");
            }

            return true;
        }

        private bool ConvertSummaryElement(HtmlElementNode htmlElementNode)
        {
            if (Options.InputMarkdownType == MarkdownType.GitHub && Options.OutputMarkdownType != MarkdownType.GitHub)
            {
                if (htmlElementNode.Name.String == "summary")
                {
                    return true;
                }
            }

            return false;
        }

        private void ConvertHtmlElement(HtmlElementNode htmlNode)
        {
            EnsureNewLineIfNotInline();

            _result.Append('<');
            string name = htmlNode.Name.String;
            _result.Append(name);

            foreach (HtmlAttributeNode htmlAttribute in htmlNode.Attributes.Values)
            {
                _result.Append(' ');

                string attrName = htmlAttribute.Name.String;
                _result.Append(attrName);
                _result.Append('=');

                string attrValue = htmlAttribute.Value.String;
                if (name == "img" && attrName == "src")
                {
                    if (Options.ImagesMap.TryGetValue(attrValue, out Image image))
                    {
                        attrValue = image.Address;
                    }
                }

                _result.Append('\"');
                _result.Append(attrValue);
                _result.Append('\"');
            }

            if (htmlNode.SelfClosingTag != null)
            {
                _result.Append(htmlNode.SelfClosingTag.Substring);
            }
            else
            {
                _result.Append('>');

                ConvertChildren(htmlNode);

                _result.Append('<');
                _result.Append('/');
                _result.Append(name);
                _result.Append('>');
            }
        }


        private void ConvertChildren(HtmlElementNode htmlElementNode)
        {
            foreach (Node child in htmlElementNode.Content)
                Convert(child, true);

            EnsureNewLineIfNotInline();
        }

        private void EnsureNewLineIfNotInline()
        {
            if (!_inline)
                _result.EnsureNewLine(_lastBlockIsMarkdown);
        }

        private void ConvertMarkdown(MarkdownNode markdownNode, bool ensureNewLine)
        {
            var markdownObject = markdownNode.MarkdownObject;
            _result.SetIndent(markdownObject.Column);

            if (ensureNewLine && !_inline)
                _result.EnsureNewLine(true);

            switch (markdownObject)
            {
                case MarkdownDocument _:
                    ConvertMarkdownDocument((MarkdownContainerBlockNode)markdownNode);
                    break;

                case HeadingBlock _:
                    ConvertHeadingBlock((MarkdownLeafBlockNode)markdownNode);
                    break;

                case ThematicBreakBlock _:
                    ConvertThematicBreakBlock((MarkdownLeafBlockNode)markdownNode);
                    break;

                case ListBlock _:
                    ConvertListBlock((MarkdownContainerBlockNode)markdownNode);
                    break;

                case ListItemBlock _:
                    ConvertListItemBlock((MarkdownContainerBlockNode)markdownNode);
                    break;

                case QuoteBlock _:
                    ConvertQuoteBlock((MarkdownContainerBlockNode)markdownNode);
                    break;

                case CodeBlock _:
                    ConvertCodeBlock((MarkdownLeafBlockNode)markdownNode);
                    break;

                case ParagraphBlock _:
                    ConvertParagraphBlock((MarkdownLeafBlockNode)markdownNode);
                    break;

                case Inline _:
                    ConvertInline(markdownNode);
                    break;

                case HtmlBlock _:
                    break;

                default:
                    throw new NotImplementedException($"Converting of Block type '{markdownObject.GetType()}' is not implemented");
            }
        }

        private void ConvertMarkdownDocument(MarkdownContainerBlockNode markdownDocument)
        {
            foreach (Node child in markdownDocument.Children)
                Convert(child, true);
        }

        private void ConvertHeadingBlock(MarkdownLeafBlockNode headingBlockNode)
        {
            _headingNumber++;

            if (_headingNumber == 0 && Options.RemoveTitleHeader)
                return;

            var headingBlock = (HeadingBlock)headingBlockNode.LeafBlock;

            if (headingBlock.HeaderChar != '\0')
            {
                _notBreak = true;
                _result.Append(headingBlock.HeaderChar, headingBlock.Level);
                _result.Append(' ');
            }

            Convert(headingBlockNode.Inline, false);

            if (headingBlock.HeaderChar != '\0')
            {
                _notBreak = false;
            }
            else
            {
                _result.AppendNewLine();
                _result.Append(headingBlock.Level == 1 ? '=' : '-', 3); // TODO: correct repeating count (extract from span)
            }
        }

        private void ConvertThematicBreakBlock(MarkdownLeafBlockNode thematicBreakBlockNode)
        {
            var thematicBreakBlock = (ThematicBreakBlock) thematicBreakBlockNode.LeafBlock;
            for (int i = 0; i < thematicBreakBlock.ThematicCharCount; i++)
                _result.Append(thematicBreakBlock.ThematicChar);
        }

        private void ConvertListBlock(MarkdownContainerBlockNode listBlockNode)
        {
            var listBlock = (ListBlock) listBlockNode.ContainerBlock;
            foreach (Node child in listBlockNode.Children)
            {
                if (child is MarkdownContainerBlockNode childMarkdownNode && childMarkdownNode.ContainerBlock is ListItemBlock listItemBlock)
                {
                    _result.SetIndent(listItemBlock.Column);
                    _result.EnsureNewLine();

                    if (listBlock.IsOrdered)
                    {
                        string orderString = listItemBlock.Order.ToString();
                        _result.Append(orderString);
                        _result.Append(listBlock.OrderedDelimiter);
                    }
                    else
                    {
                        _result.Append(listBlock.BulletType);
                    }

                    Convert(child, false);
                }
            }
        }

        private void ConvertListItemBlock(MarkdownContainerBlockNode listItemBlockNode)
        {
            var listItemBlock = (ListItemBlock) listItemBlockNode.ContainerBlock;
            for (var index = 0; index < listItemBlockNode.Children.Count; index++)
            {
                var itemBlock = listItemBlock[index];
                if (index == 0)
                    _result.Append(' ', itemBlock.Column - _result.CurrentColumn);
                else
                    _result.EnsureNewLine(!(itemBlock is ListBlock || itemBlock is QuoteBlock));

                Convert(listItemBlockNode.Children[index], false);
            }
        }

        private void ConvertQuoteBlock(MarkdownContainerBlockNode quoteBlockNode)
        {
            var quoteBlock = (QuoteBlock) quoteBlockNode.ContainerBlock;
            for (var index = 0; index < quoteBlockNode.Children.Count; index++)
            {
                Block childQuoteBlock = quoteBlock[index];
                _result.SetIndent(quoteBlock.Column);
                if (index > 0 || !(quoteBlock.Parent is QuoteBlock) && !(quoteBlock.Parent is ListItemBlock))
                {
                    _result.EnsureNewLine();
                }
                _result.Append(quoteBlock.QuoteChar);
                _result.Append(' ');
                _result.SetIndent(childQuoteBlock.Column);
                Convert(quoteBlockNode.Children[index], false);
            }
        }

        private void ConvertCodeBlock(MarkdownLeafBlockNode codeBlockNode)
        {
            var codeBlock = (CodeBlock) codeBlockNode.LeafBlock;
            FencedCodeBlock fencedCodeBlock = codeBlock as FencedCodeBlock;

            if (fencedCodeBlock != null)
            {
                _result.Append(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
                _result.Append(fencedCodeBlock.Info);
                _result.AppendNewLine();
            }

            ReadOnlySpan<char> origSpan = codeBlockNode.File.Data.AsSpan();
            var lines = codeBlock.Lines.Lines;
            for (var index = 0; index < codeBlock.Lines.Count; index++)
            {
                var line = lines[index];
                var slice = line.Slice;
                _result.SetIndent(line.Column);
                _result.Append(origSpan.Slice(slice.Start, slice.Length));
                _result.AppendNewLine();
            }

            if (fencedCodeBlock != null)
            {
                _result.SetIndent(codeBlock.Column);
                _result.Append(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
            }
        }

        private void ConvertParagraphBlock(MarkdownLeafBlockNode paragraphBlockNode)
        {
            ConvertInline(paragraphBlockNode.Inline);
        }

        private string ConvertInline(Node node, bool appendToCurrentParagraph = true)
        {
            string result = null;

            if (node is MarkdownNode markdownNode)
            {
                var inline = (Inline) markdownNode.MarkdownObject;

                switch (inline)
                {
                    case LiteralInline literalInline:
                        result = node.File.GetSubstring(literalInline.Span.Start, literalInline.Span.Length);
                        if (appendToCurrentParagraph)
                        {
                            if (IsBreakAcceptable)
                            {
                                string[] words = result.Split();
                                foreach (string word in words)
                                    AppendWithBreak(word);
                            }
                            else
                            {
                                AppendWithBreak(result);
                            }
                        }
                        break;

                    case LineBreakInline _:
                        if (Options.LinesMaxLength == 0)
                            _result.AppendNewLine();
                        break;

                    case CodeInline codeInline:
                        result = codeInline.Delimiter + codeInline.Content + codeInline.Delimiter;
                        if (appendToCurrentParagraph)
                            AppendWithBreak(result);
                        break;

                    case ContainerInline _:
                        result = ConvertContainerInline((MarkdownContainerInlineNode) markdownNode)?.ToString();
                        if (result != null && appendToCurrentParagraph)
                            AppendWithBreak(result);
                        break;

                    case AutolinkInline autolinkInline:
                        result = "<" + autolinkInline.Url + ">";
                        if (appendToCurrentParagraph)
                            AppendWithBreak(result);
                        break;

                    case HtmlInline _:
                        throw new InvalidProgramException(
                            $"Converting of Inline type '{nameof(HtmlInline)}' should be in another way");

                    default:
                        throw new NotImplementedException(
                            $"Converting of Inline type '{inline.GetType()}' is not implemented");
                }
            }

            if (node is HtmlNode htmlNode)
            {
                var converter = new Converter(Options, Logger, true);
                result = converter.ConvertAndReturn(new ParseResult(node.File, htmlNode,
                    (Dictionary<Node, Link>)_parseResult.Links, (Dictionary<string, Anchor>)_parseResult.Anchors,
                    _parseResult.EndOfLine));
                if (appendToCurrentParagraph)
                    AppendWithBreak(result);
            }

            return result;
        }

        private StringBuilder ConvertContainerInline(MarkdownContainerInlineNode containerInlineNode)
        {
            var containerInline = containerInlineNode.ContainerInline;
            var linkInline = containerInline as LinkInline;
            var emphasisInline = containerInline as EmphasisInline;
            bool appendToCurrentParagraph = false;
            StringBuilder result = null;

            if (linkInline != null)
            {
                result = new StringBuilder();
                if (linkInline.IsImage)
                    result.Append('!');
                result.Append('[');
            }
            else if (emphasisInline != null)
            {
                result = new StringBuilder();
                for (int i = 0; i < emphasisInline.DelimiterCount; i++)
                    result.Append(emphasisInline.DelimiterChar);
            }
            else if (containerInline is LinkDelimiterInline)
            {
                result = new StringBuilder();
                result.Append('[');
            }
            else
            {
                appendToCurrentParagraph = true;
            }

            foreach (Node child in containerInlineNode.Children)
            {
                string inlineResult = ConvertInline(child, appendToCurrentParagraph);
                if (!appendToCurrentParagraph)
                    result.Append(inlineResult);
            }

            if (linkInline != null)
            {
                result.Append("](");
                string url = linkInline.Url;

                var link = _parseResult.Links[containerInlineNode];
                string newAddress = null;
                if (link is RelativeLink relativeLink)
                {
                    if (Options.InputMarkdownType != Options.OutputMarkdownType)
                    {
                        newAddress = "#" + (_parseResult.Anchors.TryGetValue(relativeLink.Address, out Anchor anchor)
                            ? _headerToLinkConverter.Convert(anchor.Node, Options.OutputMarkdownType)
                            : _headerToLinkConverter.Convert(relativeLink.Address, Options.OutputMarkdownType));
                    }
                }
                else
                {
                    if (Options.ImagesMap.TryGetValue(url, out Image image))
                    {
                        url = image.Address;
                    }
                }

                result.Append(newAddress ?? url);
                result.Append(')');
            }
            else if (emphasisInline != null)
            {
                for (int i = 0; i < emphasisInline.DelimiterCount; i++)
                    result.Append(emphasisInline.DelimiterChar);
            }

            return result;
        }

        private void AppendWithBreak(string word)
        {
            int linesMaxLength = IsBreakAcceptable ? Options.LinesMaxLength : int.MaxValue;

            bool insertSpace = !_result.IsLastCharWhitespace() && _lastBlockIsMarkdown;

            if (_result.CurrentColumn + word.Length + (insertSpace ? 1 : 0) > linesMaxLength && !Consts.SpecialCharsRegex.IsMatch(word))
            {
                if (_result.CurrentColumn > 0)
                {
                    _result.AppendNewLine();
                    insertSpace = false;
                }
            }

            if (insertSpace)
                _result.Append(' ');
            _result.Append(word);
        }

        private bool IsBreakAcceptable => Options.LinesMaxLength > 0 && !_notBreak;
    }
}