using System;
using System.Text;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class MarkdownConverter
    {
        private bool _notBreak;
        private readonly ConversionResult _result;

        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        public MarkdownConverter(ProcessorOptions options, ILogger logger, ConversionResult conversionResult)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
            _result = conversionResult ?? throw new ArgumentNullException(nameof(conversionResult));
        }

        public void ConvertBlock(Block block)
        {
            _result.SetIndent(block.Column);

            switch (block)
            {
                case MarkdownDocument markdownDocument:
                    ConvertMarkdownDocument(markdownDocument);
                    break;

                case HeadingBlock headingBlock:
                    ConvertHeadingBlock(headingBlock);
                    break;

                case ThematicBreakBlock thematicBreakBlock:
                    ConvertThematicBreakBlock(thematicBreakBlock);
                    break;

                case ListBlock listBlock:
                    ConvertListBlock(listBlock);
                    break;

                case ListItemBlock listItemBlock:
                    ConvertListItemBlock(listItemBlock);
                    break;

                case QuoteBlock quoteBlock:
                    ConvertQuoteBlock(quoteBlock);
                    break;

                case CodeBlock codeBlock:
                    ConvertCodeBlock(codeBlock);
                    break;

                case HtmlBlock htmlBlock:
                    ConvertHtmlBlock(htmlBlock);
                    break;

                case ParagraphBlock paragraphBlock:
                    ConvertParagraphBlock(paragraphBlock);
                    break;

                default:
                    throw new NotImplementedException($"Converting of Block type '{block.GetType()}' is not implemented");
            }
        }

        private void ConvertMarkdownDocument(MarkdownDocument markdownDocument)
        {
            foreach (Block block in markdownDocument)
            {
                _result.EnsureNewLine(true);
                ConvertBlock(block);
            }
        }

        private void ConvertHeadingBlock(HeadingBlock headingBlock)
        {
            if (headingBlock.HeaderChar != '\0')
            {
                _notBreak = true;
                _result.Append(headingBlock.HeaderChar, headingBlock.Level);
                _result.Append(' ');
            }

            ConvertInline(headingBlock.Inline);

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

        private void ConvertThematicBreakBlock(ThematicBreakBlock thematicBreakBlock)
        {
            for (int i = 0; i < thematicBreakBlock.ThematicCharCount; i++)
            {
                _result.Append(thematicBreakBlock.ThematicChar);
            }
        }

        private void ConvertListBlock(ListBlock listBlock)
        {
            foreach (Block childListBlock in listBlock)
            {
                _result.SetIndent(childListBlock.Column);
                _result.EnsureNewLine();
                if (childListBlock is ListItemBlock listItemBlock)
                {
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

                    var converter = new Converter(Options, Logger, _result);
                    converter.ConvertContainerBlock(listItemBlock);
                }
                else
                {
                }
            }
        }

        private void ConvertListItemBlock(ListItemBlock listItemBlock)
        {
            for (var index = 0; index < listItemBlock.Count; index++)
            {
                var itemBlock = listItemBlock[index];
                if (index == 0)
                    _result.Append(' ', itemBlock.Column - _result.CurrentColumn);
                else
                    _result.EnsureNewLine();
                ConvertBlock(itemBlock);
            }
        }

        private void ConvertQuoteBlock(QuoteBlock quoteBlock)
        {
            for (var index = 0; index < quoteBlock.Count; index++)
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
                ConvertBlock(childQuoteBlock);
            }
        }

        private void ConvertCodeBlock(CodeBlock codeBlock)
        {
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

        private void ConvertHtmlBlock(HtmlBlock htmlBlock)
        {
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
                htmlData.Append("\n");
            }

            _result.Append(htmlData.ToString());
        }

        private void ConvertParagraphBlock(ParagraphBlock paragraphBlock)
        {
            ConvertInline(paragraphBlock.Inline);
        }

        private string ConvertInline(Inline inline, bool appendToCurrentParagraph = true)
        {
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

                case ContainerInline containerInline:
                    result = ConvertContainerInline(containerInline)?.ToString();
                    if (result != null && appendToCurrentParagraph)
                        AppendWithBreak(result);
                    break;

                case AutolinkInline autolinkInline:
                    result = "<" + autolinkInline.Url + ">";
                    if (appendToCurrentParagraph)
                        AppendWithBreak(result);
                    break;

                case HtmlInline htmlInline:
                    result = new Converter(Options, Logger).ConvertHtmlAndReturn(htmlInline.Tag);
                    if (appendToCurrentParagraph)
                        AppendWithBreak(result);
                    break;

                default:
                    throw new NotImplementedException($"Converting of Inline type '{inline.GetType()}' is not implemented");
            }

            return result;
        }

        private StringBuilder ConvertContainerInline(ContainerInline containerInline)
        {
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

            foreach (Inline inline2 in containerInline)
            {
                var inlineResult = ConvertInline(inline2, appendToCurrentParagraph);
                if (!appendToCurrentParagraph)
                    result.Append(inlineResult);
            }

            if (linkInline != null)
            {
                result.Append("](");
                result.Append(linkInline.Url);
                result.Append(")");
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