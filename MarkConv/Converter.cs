using System;
using System.Linq;
using System.Text;
using MarkConv.Links;
using MarkConv.Nodes;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class Converter
    {
        private static readonly string[] SpacesAndNewLines = { "\n", "\r\n", " ", "\t"};

        private readonly ProcessorOptions _options;
        private readonly ILogger _logger;
        private readonly bool _inline;

        private readonly ParseResult _parseResult;
        private readonly ConversionResult _result;
        private readonly ConverterState _converterState;

        private bool _notBreak;
        private bool _lastBlockIsMarkdown = true;

        public Converter(ProcessorOptions options, ILogger logger, ParseResult parseResult, ConverterState converterState, bool inline = false)
        {
            _options = options;
            _logger = logger;
            _inline = inline;
            _parseResult = parseResult;
            _converterState = converterState;
            _result = new ConversionResult(parseResult.EndOfLine);
        }

        public string ConvertAndReturn()
        {
            Convert(_parseResult.Node, false);
            return _result.ToString();
        }

        private string ConvertAndReturn(Node node)
        {
            Convert(node, false);
            return _result.ToString();
        }

        private void Convert(Node? node, bool ensureNewLine)
        {
            if (node is HtmlNode htmlNode)
            {
                ConvertHtmlNode(htmlNode);
                _lastBlockIsMarkdown = false;
            }
            else if (node is MarkdownNode markdownNode)
            {
                ConvertMarkdown(markdownNode, ensureNewLine);
                _lastBlockIsMarkdown = true;
            }
        }

        private void ConvertHtmlNode(HtmlNode htmlNode)
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
            var origString = htmlStringNode.String;
            var text = origString.TrimEnd();
            _lastBlockIsMarkdown = true;

            bool isFirstCharWhitespace = origString.Length > 1 && char.IsWhiteSpace(origString[0]);

            if (_options.LinesMaxLength != 0)
            {
                string[] words = text.Split(SpacesAndNewLines, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < words.Length; i++)
                    AppendWithBreak(words[i], i == 0 && !isFirstCharWhitespace);
            }
            else
            {
                AppendWithBreak(text, true);
            }

            if (origString.Length > 1 && char.IsWhiteSpace(origString[^1]) && !_result.IsLastCharWhitespaceOrLeadingPunctuation())
                _result.Append(' ');
        }

        private void ConvertHtmlComment(HtmlCommentNode htmlCommentNode)
        {
            if (!_options.RemoveComments)
            {
                EnsureNewLineIfNotInline();
                _result.Append("<!--");
                _result.Append(htmlCommentNode.Comment);
                _result.Append("-->");
            }
        }

        private bool ConvertDetailsOrSpoilerElement(HtmlElementNode htmlElementNode)
        {
            string name = htmlElementNode.Name.String.ToLowerInvariant();
            string? detailsTitle = null;
            bool removeDetails = false;
            bool convertDetails = false;

            if (_options.RemoveDetails || _options.InputMarkdownType == MarkdownType.GitHub && _options.OutputMarkdownType != MarkdownType.GitHub)
            {
                if (name == "details")
                {
                    if (htmlElementNode.TryGetChild("summary", out Node? child))
                        detailsTitle = (child as HtmlElementNode)?.Content.FirstOrDefault()?.Substring;
                    AssignRemoveOrConvert();
                }
            }
            else if (_options.RemoveDetails || _options.InputMarkdownType == MarkdownType.Habr && _options.OutputMarkdownType != MarkdownType.Habr)
            {
                if (name == "spoiler")
                {
                    if (htmlElementNode.TryGetChild("title", out Node? child))
                        detailsTitle = (child as HtmlElementNode)?.Content.FirstOrDefault()?.Substring;
                    AssignRemoveOrConvert();
                }
            }

            void AssignRemoveOrConvert()
            {
                if (_options.RemoveDetails)
                    removeDetails = true;
                else
                    convertDetails = true;
            }

            if (removeDetails)
                return true;

            if (!convertDetails)
                return false;

            EnsureNewLineIfNotInline();

            if (_options.OutputMarkdownType == MarkdownType.GitHub)
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
            else if (_options.OutputMarkdownType == MarkdownType.Habr)
            {
                _result.Append("<spoiler");
                if (detailsTitle != null)
                    _result.Append($" title=\"{detailsTitle}\"");
                _result.Append('>');
                _result.AppendNewLine();

                ConvertChildren(htmlElementNode);

                _result.Append("</spoiler>");
            }
            else if (_options.OutputMarkdownType == MarkdownType.Dev)
            {
                _result.Append("{% details");
                if (detailsTitle != null)
                {
                    _result.Append(' ');
                    _result.Append(detailsTitle);
                }

                _result.Append(" %}");
                _result.AppendNewLine();

                ConvertChildren(htmlElementNode);

                _result.Append("{% enddetails %}");
            }

            return true;
        }

        private bool ConvertSummaryElement(HtmlElementNode htmlElementNode)
        {
            if (_options.InputMarkdownType == MarkdownType.GitHub && _options.OutputMarkdownType != MarkdownType.GitHub)
            {
                if (htmlElementNode.Name.String.Equals("summary", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void ConvertHtmlElement(HtmlElementNode htmlNode)
        {
            string name = htmlNode.Name.String.ToLowerInvariant();
            bool formattingElement = name == "a" || name == "b" || name == "i" || name == "s" || name == "summary";

            if (!formattingElement)
                EnsureNewLineIfNotInline();

            string? headerImageLink = null;

            if (name == "img")
            {
                _converterState.ImageLinkNumber++;

                if (_converterState.ImageLinkNumber == 0 && !string.IsNullOrWhiteSpace(_options.HeaderImageLink))
                {
                    headerImageLink = _options.HeaderImageLink;
                    _result.Append('[');
                }
            }

            _result.Append('<');
            _result.Append(name);

            foreach (HtmlAttributeNode htmlAttribute in htmlNode.Attributes.Values)
            {
                _result.Append(' ');

                string attrName = htmlAttribute.Name.String.ToLowerInvariant();
                _result.Append(attrName);
                _result.Append('=');

                string attrValue = htmlAttribute.Value.String;
                if (name == "img" && attrName == "src")
                {
                    if (_options.ImagesMap.TryGetValue(attrValue, out Image? image))
                    {
                        attrValue = image.Address;
                    }
                }

                _result.Append('\"');
                _result.Append(attrValue);
                _result.Append('\"');
            }

            if (!htmlNode.SelfClosing)
            {
                _result.Append('>');
                if (!formattingElement)
                    _result.AppendNewLine();
                ConvertChildren(htmlNode, !formattingElement);
            }

            _result.Append(htmlNode.ClosingTag.Substring.ToLowerInvariant());

            if (headerImageLink != null)
            {
                _result.Append("](");
                _result.Append(headerImageLink);
                _result.Append(")");
            }
        }

        private void ConvertChildren(HtmlElementNode htmlElementNode, bool ensureNewLine = true)
        {
            foreach (Node child in htmlElementNode.Content)
                Convert(child, true);

            if (ensureNewLine)
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
                    throw new ArgumentException($"{markdownObject.GetType()} should be converter in {nameof(ConvertListBlock)} method");

                case QuoteBlock _:
                    ConvertQuoteBlock((MarkdownContainerBlockNode)markdownNode);
                    break;

                case CodeBlock _:
                    ConvertCodeBlock((MarkdownLeafBlockNode)markdownNode);
                    break;

                case Table _:
                    ConvertTable((MarkdownContainerBlockNode)markdownNode);
                    break;

                case ParagraphBlock _:
                    ConvertParagraphBlock((MarkdownLeafBlockNode)markdownNode);
                    break;

                case Inline _:
                    ConvertInline(markdownNode);
                    break;

                case TableRow _:
                case TableCell _:
                    throw new ArgumentException($"{markdownObject.GetType()} should be converter in {nameof(ConvertTable)} method");

                case HtmlBlock _:
                    break;

                default:
                    throw new NotImplementedException($"Converting of Block type '{markdownObject.GetType()}' is not implemented");
            }
        }

        private void ConvertMarkdownDocument(MarkdownContainerBlockNode markdownDocument)
        {
            foreach (Node child in markdownDocument.Children)
            {
                _result.SetIndent(markdownDocument.MarkdownObject.Column);
                Convert(child, true);
            }
        }

        private void ConvertTable(MarkdownContainerBlockNode tableBlockNode)
        {
            var table = (Table) tableBlockNode.MarkdownObject;
            for (var rowIndex = 0; rowIndex < tableBlockNode.Children.Count; rowIndex++)
            {
                Node row = tableBlockNode.Children[rowIndex];
                var rowNode = (MarkdownContainerBlockNode) row;
                var rowMarkdown = (TableRow) rowNode.MarkdownObject;
                _result.SetIndent(rowMarkdown.Column - 1);
                _result.Append("|");

                foreach (Node cell in rowNode.Children)
                {
                    var cellNode = (MarkdownContainerBlockNode) cell;
                    foreach (Node child in cellNode.Children)
                        Convert(child, false);
                    _result.Append("|");
                }
                if (rowIndex < tableBlockNode.Children.Count - 1)
                    _result.AppendNewLine();

                if (rowIndex == 0)
                {
                    // Add separator
                    _result.SetIndent(rowMarkdown.Column - 1);
                    _result.Append('|');
                    for (var columnIndex = 0; columnIndex < table.ColumnDefinitions.Count - 1; columnIndex++)
                        _result.Append("---|");
                    _result.AppendNewLine();
                }
            }
        }

        private void ConvertHeadingBlock(MarkdownLeafBlockNode headingBlockNode)
        {
            _converterState.HeadingNumber++;

            if (_converterState.HeadingNumber == 0 && _options.RemoveTitleHeader)
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
                var headingStr = headingBlockNode.Substring.TrimEnd();
                int wsIndex = headingStr.Length - 1;
                while (wsIndex > 0)
                {
                    if (char.IsWhiteSpace(headingStr[wsIndex]))
                        break;
                    wsIndex--;
                }
                int headingSeparatorCharCount = headingStr.Length - wsIndex - 1;
                _result.Append(headingBlock.Level == 1 ? '=' : '-', headingSeparatorCharCount); // TODO: correct repeating count (extract from span)
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
                var childMarkdownNode = (MarkdownContainerBlockNode) child;
                var listItemBlock = (ListItemBlock) childMarkdownNode.ContainerBlock;

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

                var children = childMarkdownNode.Children;
                for (var index = 0; index < children.Count; index++)
                {
                    var itemBlock = listItemBlock[index];
                    if (index == 0)
                        _result.Append(' ', itemBlock.Column - _result.CurrentColumn);
                    else
                        _result.EnsureNewLine(!(itemBlock is ListBlock || itemBlock is QuoteBlock));

                    Convert(children[index], false);
                }
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
                    _result.EnsureNewLine();
                _result.Append(quoteBlock.QuoteChar);
                _result.Append(' ');
                _result.SetIndent(childQuoteBlock.Column);
                Convert(quoteBlockNode.Children[index], false);
            }
        }

        private void ConvertCodeBlock(MarkdownLeafBlockNode codeBlockNode)
        {
            var codeBlock = (CodeBlock) codeBlockNode.LeafBlock;
            FencedCodeBlock? fencedCodeBlock = codeBlock as FencedCodeBlock;

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

        private string? ConvertInline(Node? node, bool appendToCurrentParagraph = true)
        {
            string? result = null;

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
                        result = " ";
                        if (_options.LinesMaxLength == 0)
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

                _lastBlockIsMarkdown = true;
            }
            else if (node is HtmlNode htmlNode)
            {
                var converter = new Converter(_options, _logger, _parseResult, _converterState, true);
                result = converter.ConvertAndReturn(htmlNode);
                if (appendToCurrentParagraph)
                    AppendWithBreak(result);
            }

            return result;
        }

        private StringBuilder? ConvertContainerInline(MarkdownContainerInlineNode containerInlineNode)
        {
            var containerInline = containerInlineNode.ContainerInline;
            LinkInline? linkInline = containerInline as LinkInline;
            EmphasisInline? emphasisInline = containerInline as EmphasisInline;
            bool appendToCurrentParagraph = false;
            StringBuilder? result = null;
            string? headerImageLink = null;

            if (linkInline != null)
            {
                result = new StringBuilder();
                if (linkInline.IsImage)
                {
                    _converterState.ImageLinkNumber++;

                    if (_converterState.ImageLinkNumber == 0 && !string.IsNullOrWhiteSpace(_options.HeaderImageLink))
                    {
                        headerImageLink = _options.HeaderImageLink;
                        result.Append('[');
                    }

                    result.Append('!');
                }
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
                string? inlineResult = ConvertInline(child, appendToCurrentParagraph);
                if (!appendToCurrentParagraph)
                    result!.Append(inlineResult);
            }

            if (linkInline != null)
            {
                result!.Append("](");
                string url = linkInline.Url;

                var link = _parseResult.Links[containerInlineNode];
                string? newAddress = null;
                if (link is RelativeLink relativeLink)
                {
                    if (_options.InputMarkdownType != _options.OutputMarkdownType)
                    {
                        newAddress = "#" + (_parseResult.Anchors.TryGetValue(relativeLink.Address, out Anchor? anchor)
                            ? _converterState.HeaderToLinkConverter.Convert(anchor.Node, _options.OutputMarkdownType)
                            : _converterState.HeaderToLinkConverter.Convert(relativeLink.Address, _options.OutputMarkdownType));
                    }
                }
                else
                {
                    if (_options.ImagesMap.TryGetValue(url, out Image? image))
                    {
                        url = image.Address;
                    }
                }

                result.Append(newAddress ?? url);
                result.Append(')');

                if (headerImageLink != null)
                {
                    result.Append("](");
                    result.Append(headerImageLink);
                    result.Append(')');
                }
            }
            else if (emphasisInline != null)
            {
                for (int i = 0; i < emphasisInline.DelimiterCount; i++)
                    result!.Append(emphasisInline.DelimiterChar);
            }

            return result;
        }

        private void AppendWithBreak(string word, bool forceNotInsertSpace = false)
        {
            if (string.IsNullOrEmpty(word))
                return;

            int linesMaxLength = IsBreakAcceptable ? _options.LinesMaxLength : int.MaxValue;

            bool insertSpace = !forceNotInsertSpace &&
                               !_result.IsLastCharWhitespaceOrLeadingPunctuation() &&
                               !char.IsWhiteSpace(word[0]) && !IsTrailingPunctuation(word) &&
                               _lastBlockIsMarkdown;

            if (_result.CurrentColumn + word.Length + (insertSpace ? 1 : 0) > linesMaxLength && !IsLineStartingSyntax(word))
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

        private bool IsLineStartingSyntax(string word)
        {
            if (word.Length == 1)
            {
                char c = word[0];
                return c == '>' || c == '*' || c == '-' || c == '+' || c == '|' || c == '=';
            }

            // Check \d+. pattern
            for (int i = 0; i < word.Length - 1; i++)
                if (!char.IsDigit(word[i]))
                    return false;

            return word[^1] == '.';
        }

        private bool IsBreakAcceptable => _options.LinesMaxLength > 0 && !_notBreak;

        private static bool IsTrailingPunctuation(string word)
        {
            if (word.Length == 0)
                return true;

            char c = word[0];
            if (c == ',' || c == '.' || c == ';' || c == ':' || c == '?' || c == '\'' || c == '"' ||
                c == ')' || c == '}' || c == ']')
            {
                return true;
            }

            if (c == '!')
            {
                return word.Length == 1 || word[1] != '[';
            }

            return false;
        }
    }
}