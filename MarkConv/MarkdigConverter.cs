using System;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkConv
{
    public class MarkdigConverter
    {
        private const string NewLine = "\n";
        private bool _notBreak;
        private int _currentColumn;
        private int _currentIndent; // TODO: normalize indents
        private HtmlConverter _htmlConverter;
        private StringBuilder _result;

        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        public MarkdigConverter(ProcessorOptions options = null, ILogger logger = null)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
            _htmlConverter = new HtmlConverter(options, logger);
        }

        public string Convert(string mdContent)
        {
            MarkdownDocument doc = Markdown.Parse(mdContent);

            _result = new StringBuilder(mdContent.Length);

            foreach (Block child in doc)
            {
                EnsureNewLine(true);
                ConvertBlock(child);
            }

            // Trim whitespaces and newlines
            int lastWsIndex = _result.Length - 1;
            while (lastWsIndex >= 0)
            {
                if (!char.IsWhiteSpace(_result[lastWsIndex]))
                    break;
                lastWsIndex--;
            }

            lastWsIndex++;
            if (lastWsIndex < _result.Length)
                _result.Remove(lastWsIndex, _result.Length - lastWsIndex);

            return _result.ToString();
        }

        private void ConvertBlock(Block block)
        {
            _currentIndent = block.Column;

            switch (block)
            {
                case HeadingBlock headingBlock:
                    ConvertHeadingBlock(headingBlock);
                    break;

                case ThematicBreakBlock thematicBreakBlock:
                    ConvertThematicBreakBlock(thematicBreakBlock);
                    break;

                case ListBlock listBlock:
                    ConvertListBlock(listBlock);
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

        private void ConvertHeadingBlock(HeadingBlock headingBlock)
        {
            if (headingBlock.HeaderChar != '\0')
            {
                _notBreak = true;
                Append(headingBlock.HeaderChar, headingBlock.Level);
                Append(' ');
            }

            ConvertInline(headingBlock.Inline);

            if (headingBlock.HeaderChar != '\0')
            {
                _notBreak = false;
            }
            else
            {
                AppendNewLine();
                Append(headingBlock.Level == 1 ? '=' : '-', 3); // TODO: correct repeating count (extract from span)
            }
        }

        private void ConvertThematicBreakBlock(ThematicBreakBlock thematicBreakBlock)
        {
            for (int i = 0; i < thematicBreakBlock.ThematicCharCount; i++)
            {
                Append(thematicBreakBlock.ThematicChar);
            }
        }

        private void ConvertListBlock(ListBlock listBlock)
        {
            foreach (Block childListBlock in listBlock)
            {
                _currentIndent = childListBlock.Column;
                EnsureNewLine();
                if (childListBlock is ListItemBlock listItemBlock)
                {
                    int appendWidth;
                    if (listBlock.IsOrdered)
                    {
                        string orderString = listItemBlock.Order.ToString();
                        Append(orderString);
                        Append(listBlock.OrderedDelimiter);
                        appendWidth = orderString.Length + 1;
                    }
                    else
                    {
                        Append(listBlock.BulletType);
                        appendWidth = 1;
                    }

                    for (var index = 0; index < listItemBlock.Count; index++)
                    {
                        var itemBlock = listItemBlock[index];
                        if (index == 0)
                            Append(' ', itemBlock.Column - listItemBlock.Column - appendWidth);
                        else
                            EnsureNewLine();
                        _currentIndent = itemBlock.Column;
                        ConvertBlock(itemBlock);
                    }
                }
                else
                {
                }
            }
        }

        private void ConvertQuoteBlock(QuoteBlock quoteBlock)
        {
            for (var index = 0; index < quoteBlock.Count; index++)
            {
                Block childQuoteBlock = quoteBlock[index];
                _currentIndent = quoteBlock.Column;
                if (index > 0 || !(quoteBlock.Parent is QuoteBlock) && !(quoteBlock.Parent is ListItemBlock))
                {
                    EnsureNewLine();
                }
                Append(quoteBlock.QuoteChar);
                Append(' ');
                _currentIndent = childQuoteBlock.Column;
                ConvertBlock(childQuoteBlock);
            }
        }

        private void ConvertCodeBlock(CodeBlock codeBlock)
        {
            FencedCodeBlock fencedCodeBlock = codeBlock as FencedCodeBlock;

            if (fencedCodeBlock != null)
            {
                Append(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
                Append(fencedCodeBlock.Info);
                AppendNewLine();
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

                _currentIndent = line.Column;
                Append(origSpan.Slice(slice.Start, slice.Length));
                AppendNewLine();
            }

            if (fencedCodeBlock != null)
            {
                _currentIndent = codeBlock.Column;
                Append(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
            }
        }

        private void ConvertHtmlBlock(HtmlBlock htmlBlock)
        {
            var htmlData = new StringBuilder();

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
                htmlData.Append(NewLine);
            }

            var htmlOutput = _htmlConverter.Convert(htmlData.ToString());
            Append(htmlOutput);
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
                        AppendNewLine();
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
                    result = _htmlConverter.Convert(htmlInline.Tag);
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

            bool insertSpace = _result.Length > 0 && !char.IsWhiteSpace(_result[^1]);

            if (_currentColumn + word.Length + (insertSpace ? 1 : 0) > linesMaxLength && !MarkdownRegex.SpecialCharsRegex.IsMatch(word))
            {
                if (_currentColumn > 0)
                {
                    AppendNewLine();
                    insertSpace = false;
                }
            }

            if (insertSpace)
                Append(' ');
            Append(word);
        }

        private bool IsBreakAcceptable => Options.LinesMaxLength > 0 && !_notBreak;

        private void Append(string str)
        {
            AppendIndent();
            _result.Append(str);
            _currentColumn += str.Length;
        }

        private void Append(ReadOnlySpan<char> str)
        {
            AppendIndent();
            _result.Append(str);
            _currentColumn += str.Length;
        }

        private void Append(char c)
        {
            AppendIndent();
            _result.Append(c);
            _currentColumn += 1;
        }

        private void Append(char c, int count)
        {
            AppendIndent();
            _result.Append(c, count);
            _currentColumn += count;
        }

        private void AppendIndent()
        {
            if (_result.Length > 0 && _result[^1] == '\n' && _currentIndent > 0)
            {
                _result.Append(' ', _currentIndent);
                _currentColumn = _currentIndent;
            }
        }

        private void EnsureNewLine(bool doubleNl = false)
        {
            if (doubleNl)
            {
                if (_result.Length < 2)
                    return;

                if (_result[^1] != '\n')
                {
                    AppendNewLine();
                    AppendNewLine();
                }

                if (_result[^2] != '\n')
                    AppendNewLine();
            }
            else
            {
                if (_result.Length < 1)
                    return;

                if (_result[^1] != '\n')
                    AppendNewLine();
            }
        }

        private void AppendNewLine()
        {
            _result.Append(NewLine);
            _currentColumn = 0;
        }
    }
}