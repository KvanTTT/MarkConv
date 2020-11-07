using System;
using System.Text;
using HtmlAgilityPack;
using MarkConv.Nodes;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class Converter2
    {
        private ILogger Logger { get; }

        private ProcessorOptions Options { get; }

        private bool _notBreak;
        private bool _lastBlockIsMarkdown;

        private readonly ConversionResult _result = new ConversionResult();

        public Converter2(ProcessorOptions options, ILogger logger)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
        }

        public string ConvertAndReturn(Node node)
        {
            Convert(node);
            return _result.ToString();
        }

        private void Convert(Node node, bool htmlClosing = false)
        {
            if (node is HtmlMarkdownNode htmlNode)
            {
                ConvertHtml(htmlNode, htmlClosing);
                if (htmlNode.Object?.Name != "#artificial")
                    _lastBlockIsMarkdown = false;
            }
            else if (node is MarkdownNode markdownNode)
            {
                ConvertMarkdown(markdownNode);
                _lastBlockIsMarkdown = true;
            }
        }

        private void ConvertHtml(HtmlMarkdownNode htmlMarkdownNode, bool closing = false)
        {
            var htmlNode = htmlMarkdownNode?.Object;

            if (htmlNode == null)
            {
                return;
            }

            if (ConvertHtmlTextNode(htmlMarkdownNode))
            {
                return;
            }

            if (ConvertDetailsOrSpoilerElement(htmlMarkdownNode))
            {
                return;
            }

            if (ConvertSummaryElement(htmlMarkdownNode))
            {
                return;
            }

            if (ConvertHtmlComment(htmlMarkdownNode))
            {
                return;
            }

            if (htmlNode.Name != "#document" && htmlNode.Name != "#artificial")
            {
                ConvertHtmlElement(htmlNode, closing);
            }

            ConvertChildren(htmlMarkdownNode);

            if (htmlNode.Name != "#document" && htmlNode.Name != "#artificial" && htmlNode.EndNode != htmlNode)
            {
                Convert(htmlMarkdownNode.EndNode, true);
            }
        }

        private bool ConvertHtmlTextNode(HtmlMarkdownNode htmlMarkdownNode)
        {
            if (htmlMarkdownNode.Object is HtmlTextNode htmlTextNode)
            {
                _result.Append(htmlTextNode.Text);
                return true;
            }

            return false;
        }

        private void ConvertHtmlElement(HtmlNode htmlNode, bool closing)
        {
            string name = htmlNode.Name;

            if (_lastBlockIsMarkdown)
                _result.EnsureNewLine(true);

            _result.Append('<');

            if (closing)
            {
                _result.Append('/');
            }

            _result.Append(name);

            foreach (HtmlAttribute htmlAttribute in htmlNode.Attributes)
            {
                ConvertAttribute(htmlAttribute.Name, htmlAttribute.Value, htmlAttribute.QuoteType);
            }

            if (htmlNode.EndNode == htmlNode)
            {
                _result.Append('/');
            }

            _result.Append('>');
        }

        private bool ConvertDetailsOrSpoilerElement(HtmlMarkdownNode htmlMarkdownNode)
        {
            var htmlNode = htmlMarkdownNode.Object;
            string name = htmlNode.Name;
            string detailsTitle = null;
            bool removeDetails = false;
            bool convertDetails = false;

            if (Options.RemoveDetails || Options.InputMarkdownType == MarkdownType.GitHub && Options.OutputMarkdownType != MarkdownType.GitHub)
            {
                if (name == "details")
                {
                    detailsTitle = htmlNode.ChildNodes["summary"]?.InnerText;
                    AssignRemoveOrConvert();
                }
            }
            else if (Options.RemoveDetails || Options.InputMarkdownType == MarkdownType.Habr && Options.OutputMarkdownType != MarkdownType.Habr)
            {
                if (name == "spoiler")
                {
                    detailsTitle = htmlNode.Attributes["title"]?.Value;
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
            {
                return true;
            }

            if (!convertDetails)
            {
                return false;
            }

            _result.EnsureNewLine(_lastBlockIsMarkdown);

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

                ConvertChildren(htmlMarkdownNode);

                _result.EnsureNewLine();
                _result.Append("</details>");
            }
            else if (Options.OutputMarkdownType == MarkdownType.Habr)
            {
                _result.Append("<spoiler");
                if (detailsTitle != null)
                {
                    _result.Append($" title=\"{detailsTitle}\"");
                }
                _result.Append('>');

                ConvertChildren(htmlMarkdownNode);

                _result.EnsureNewLine();
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

                ConvertChildren(htmlMarkdownNode);

                _result.EnsureNewLine();
                _result.Append("{% enddetails %}");
            }

            return true;
        }

        private bool ConvertSummaryElement(HtmlMarkdownNode htmlMarkdownNode)
        {
            if (Options.InputMarkdownType == MarkdownType.GitHub && Options.OutputMarkdownType != MarkdownType.GitHub)
            {
                if (htmlMarkdownNode.Object.Name == "summary")
                {
                    return true;
                }
            }

            return false;
        }

        private bool ConvertHtmlComment(HtmlMarkdownNode htmlMarkdownNode)
        {
            if (htmlMarkdownNode.Object is HtmlCommentNode htmlCommentNode)
            {
                if (!Options.RemoveComments)
                {
                    _result.EnsureNewLine(_lastBlockIsMarkdown);
                    _result.Append(htmlCommentNode.Comment);
                }

                return true;
            }

            return false;
        }

        private void ConvertChildren(HtmlMarkdownNode htmlMarkdownNode)
        {
            foreach (Node child in htmlMarkdownNode.Children)
            {
                /*if (child is MarkdownNode markdownNode)
                {
                    var parent = (markdownNode.Object as Block)?.Parent;
                    if (parent is ListItemBlock)
                        ; //_result.Append(' ', markdownBlock.Column - _result.CurrentColumn);
                    else
                        _result.EnsureNewLine(true);
                }*/

                Convert(child);
            }
        }

        private void ConvertAttribute(string key, string value, AttributeValueQuote? attributeValueQuote)
        {
            _result.Append(' ');
            char quote = attributeValueQuote == AttributeValueQuote.SingleQuote ? '\'' :
                attributeValueQuote == AttributeValueQuote.DoubleQuote ? '"' : '\0';

            _result.Append(key);
            _result.Append('=');

            if (quote != '\0')
                _result.Append(quote);

            _result.Append(value);

            if (quote != '\0')
                _result.Append(quote);
        }

        private void ConvertMarkdown(MarkdownNode markdownNode)
        {
            var markdownObject = markdownNode.Object;
            _result.SetIndent(markdownObject.Column);

            switch (markdownObject)
            {
                case MarkdownDocument _:
                    ConvertMarkdownDocument(markdownNode);
                    break;

                case HeadingBlock _:
                    ConvertHeadingBlock(markdownNode);
                    break;

                case ThematicBreakBlock _:
                    ConvertThematicBreakBlock(markdownNode);
                    break;

                case ListBlock _:
                    ConvertListBlock(markdownNode);
                    break;

                case ListItemBlock _:
                    ConvertListItemBlock(markdownNode);
                    break;

                case QuoteBlock _:
                    ConvertQuoteBlock(markdownNode);
                    break;

                case CodeBlock _:
                    ConvertCodeBlock(markdownNode);
                    break;

                case HtmlBlock _:
                    ConvertHtmlBlock(markdownNode);
                    break;

                case ParagraphBlock _:
                    ConvertParagraphBlock(markdownNode);
                    break;

                case Inline _:
                    ConvertInline(markdownNode);
                    break;

                default:
                    throw new NotImplementedException($"Converting of Block type '{markdownObject.GetType()}' is not implemented");
            }
        }

        private void ConvertMarkdownDocument(MarkdownNode markdownDocumentNode)
        {
            foreach (Node child in markdownDocumentNode.Children)
            {
                _result.EnsureNewLine(true);
                Convert(child);
            }
        }

        private void ConvertHeadingBlock(MarkdownNode headingBlockNode)
        {
            var headingBlock = (HeadingBlock)headingBlockNode.Object;

            if (headingBlock.HeaderChar != '\0')
            {
                _notBreak = true;
                _result.Append(headingBlock.HeaderChar, headingBlock.Level);
                _result.Append(' ');
            }

            Convert(headingBlockNode.Children[0]);

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

        private void ConvertThematicBreakBlock(MarkdownNode thematicBreakBlockNode)
        {
            var thematicBreakBlock = (ThematicBreakBlock) thematicBreakBlockNode.Object;
            for (int i = 0; i < thematicBreakBlock.ThematicCharCount; i++)
                _result.Append(thematicBreakBlock.ThematicChar);
        }

        private void ConvertListBlock(MarkdownNode listBlockNode)
        {
            var listBlock = (ListBlock) listBlockNode.Object;
            foreach (Node child in listBlockNode.Children)
            {
                if (child is MarkdownNode childMarkdownNode && childMarkdownNode.Object is ListItemBlock listItemBlock)
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

                    Convert(child);
                }
            }
        }

        private void ConvertListItemBlock(MarkdownNode listItemBlockNode)
        {
            var listItemBlock = (ListItemBlock) listItemBlockNode.Object;
            for (var index = 0; index < listItemBlockNode.Children.Count; index++)
            {
                var itemBlock = listItemBlock[index];
                if (index == 0)
                    _result.Append(' ', itemBlock.Column - _result.CurrentColumn);
                else
                    _result.EnsureNewLine();
                Convert(listItemBlockNode.Children[index]);
            }
        }

        private void ConvertQuoteBlock(MarkdownNode quoteBlockNode)
        {
            var quoteBlock = (QuoteBlock) quoteBlockNode.Object;
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
                Convert(quoteBlockNode.Children[index]);
            }
        }

        private void ConvertCodeBlock(MarkdownNode codeBlockNode)
        {
            var codeBlock = (CodeBlock) codeBlockNode.Object;
            FencedCodeBlock fencedCodeBlock = codeBlock as FencedCodeBlock;

            if (fencedCodeBlock != null)
            {
                _result.Append(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
                _result.Append(fencedCodeBlock.Info);
                _result.AppendNewLine();
            }

            ReadOnlySpan<char> origSpan = default;
            var lines = codeBlock.Lines.Lines;
            for (var index = 0; index < codeBlock.Lines.Count; index++)
            {
                var line = lines[index];
                var slice = line.Slice;
                if (origSpan == default)
                {
                    origSpan = slice.Text.AsSpan();
                }

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

        private void ConvertHtmlBlock(MarkdownNode htmlBlockNode)
        {
            var htmlBlock = (HtmlBlock) htmlBlockNode.Object;
            var htmlData = new StringBuilder(htmlBlock.Span.Length);

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
                htmlData.Append(origSpan.Slice(slice.Start, slice.Length));
                htmlData.Append('\n');
            }

            _result.Append(htmlData.ToString());
        }

        private void ConvertParagraphBlock(MarkdownNode paragraphBlockNode)
        {
            ConvertInline((MarkdownNode)paragraphBlockNode.Children[0]);
        }

        private string ConvertInline(MarkdownNode markdownNode, bool appendToCurrentParagraph = true)
        {
            var inline = (Inline) markdownNode.Object;
            string result = null;

            switch (inline)
            {
                case LiteralInline literalInline:
                    result = literalInline.ToString();
                    if (appendToCurrentParagraph)
                    {
                        if (IsBreakAcceptable)
                        {
                            string[] words = result.Split();
                            foreach (string word in words)
                            {
                                AppendWithBreak(word);
                            }
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
                    result = ConvertContainerInline(markdownNode)?.ToString();
                    if (result != null && appendToCurrentParagraph)
                        AppendWithBreak(result);
                    break;

                case AutolinkInline autolinkInline:
                    result = "<" + autolinkInline.Url + ">";
                    if (appendToCurrentParagraph)
                        AppendWithBreak(result);
                    break;

                case HtmlInline _:
                    var converter = new Converter2(Options, Logger);
                    result = converter.ConvertAndReturn(markdownNode.Children[0]);
                    if (appendToCurrentParagraph)
                        AppendWithBreak(result);
                    break;

                default:
                    throw new NotImplementedException($"Converting of Inline type '{inline.GetType()}' is not implemented");
            }

            return result;
        }

        private StringBuilder ConvertContainerInline(MarkdownNode containerInlineNode)
        {
            var containerInline = (ContainerInline) containerInlineNode.Object;
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

            foreach (Node inline2 in containerInlineNode.Children)
            {
                var inlineResult = ConvertInline((MarkdownNode)inline2, appendToCurrentParagraph);
                if (!appendToCurrentParagraph)
                    result.Append(inlineResult);
            }

            if (linkInline != null)
            {
                result.Append("](");
                result.Append(linkInline.Url);
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

            bool insertSpace = !_result.IsLastCharWhitespace();

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