using System;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkdigParserTest
{
    public class MarkdownVisitor
    {
        private const string NewLine = "\n";
        private const string NewLine2 = "\n\n";
        private StringBuilder _result;

        public string Convert(string mdContent)
        {
            MarkdownDocument doc = Markdown.Parse(mdContent);

            _result = new StringBuilder(mdContent.Length);

            foreach (Block child in doc)
            {
                ProcessBlock(child);
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

        private void ProcessBlock(Block block, bool singleNewLineAfterParagraph = false)
        {
            switch (block)
            {
                case ParagraphBlock paragraphBlock:
                    ProcessParagraphBlock(singleNewLineAfterParagraph, paragraphBlock);
                    break;

                case QuoteBlock quoteBlock:
                    ProcessQuoteBlock(quoteBlock);
                    break;

                case ListBlock listBlock:
                    ProcessListBlock(listBlock);
                    break;

                case HeadingBlock headingBlock:
                    ProcessHeadingBlock(headingBlock);
                    break;

                case HtmlBlock htmlBlock:
                    ProcessHtml(htmlBlock);
                    break;

                case CodeBlock codeBlock:
                    ProcessCodeBlock(codeBlock);
                    break;

                case ThematicBreakBlock thematicBreakBlock:
                    ProcessThematicBreakBlock(thematicBreakBlock);
                    break;

                default:
                    throw new NotImplementedException($"Processing of Block type '{block.GetType()}' is not implemented");
            }
        }

        private void ProcessInline(Inline inline)
        {
            switch (inline)
            {
                case LiteralInline literalInline:
                    _result.Append(literalInline);
                    break;

                case LineBreakInline _:
                    _result.Append(NewLine);
                    break;

                case CodeInline codeInline:
                    _result.Append(codeInline.Delimiter);
                    _result.Append(codeInline.Content);
                    _result.Append(codeInline.Delimiter);
                    break;

                case ContainerInline containerInline:
                    ProcessContainerInline(containerInline);
                    break;

                case AutolinkInline autolinkInline:
                    _result.Append('<');
                    _result.Append(autolinkInline.Url);
                    _result.Append('>');
                    break;

                default:
                    throw new NotImplementedException($"Processing of Inline type '{inline.GetType()}' is not implemented");
            }
        }

        private void ProcessParagraphBlock(bool singleNewLineAfterParagraph, ParagraphBlock paragraphBlock)
        {
            //_result.Append(new string(' ', paragraphBlock.Column));
            ProcessInline(paragraphBlock.Inline);
            _result.Append(singleNewLineAfterParagraph ? NewLine : NewLine2);
        }

        private void ProcessQuoteBlock(QuoteBlock quoteBlock)
        {
            foreach (Block childQuoteBlock in quoteBlock)
            {
                _result.Append(quoteBlock.QuoteChar);
                _result.Append(' ');
                ProcessBlock(childQuoteBlock);
            }
        }

        private void ProcessListBlock(ListBlock listBlock)
        {
            foreach (Block childListBlock in listBlock)
            {
                if (childListBlock is ListItemBlock listItemBlock)
                {
                    _result.Append(new string(' ', listItemBlock.Column));

                    int columnWidth = listItemBlock.ColumnWidth;
                    int appendWidth;
                    if (listBlock.IsOrdered)
                    {
                        string orderString = listItemBlock.Order.ToString();
                        _result.Append(orderString);
                        _result.Append(listBlock.OrderedDelimiter);
                        appendWidth = orderString.Length + 1;
                    }
                    else
                    {
                        _result.Append(listBlock.BulletType);
                        appendWidth = 1;
                    }

                    _result.Append(' ', columnWidth - appendWidth);

                    foreach (var itemBlock in listItemBlock)
                    {
                        // TODO: correct indentation
                        ProcessBlock(itemBlock, true);
                    }
                }
                else
                {
                }
            }

            _result.Append(NewLine);
        }

        private void ProcessHeadingBlock(HeadingBlock headingBlock)
        {
            _result.Append(new string(headingBlock.HeaderChar, headingBlock.Level));
            _result.Append(' ');
            ProcessInline(headingBlock.Inline);
            _result.Append(NewLine2);
        }

        private void ProcessHtml(HtmlBlock htmlBlock)
        {
            var htmlData = new StringBuilder();
            var lines = htmlBlock.Lines.Lines;
            foreach (StringLine line in lines)
            {
                htmlData.Append(line.ToString());
                htmlData.Append(NewLine);
            }

            var doc = new HtmlDocument();
            using var stringReader = new StringReader(htmlData.ToString());
            doc.Load(stringReader);

            var htmlOutput = doc.DocumentNode.OuterHtml;

            _result.Append(htmlOutput);
            _result.Append(NewLine);
        }

        private void ProcessCodeBlock(CodeBlock codeBlock)
        {
            FencedCodeBlock fencedCodeBlock = codeBlock as FencedCodeBlock;
            string fencedString = null;

            if (fencedCodeBlock != null)
            {
                fencedString = new string(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
                _result.Append(fencedString);
                _result.Append(' ');
                _result.Append(fencedCodeBlock.Info);
                _result.Append(NewLine);
            }

            for (int i = 0; i < codeBlock.Lines.Count; i++)
            {
                if (fencedCodeBlock == null)
                {
                    _result.Append("    ");
                }

                _result.Append(codeBlock.Lines.Lines[i].ToString());
                _result.Append(NewLine);
            }

            if (fencedCodeBlock != null)
            {
                _result.Append(fencedString);
                _result.Append(NewLine2);
            }
        }

        private void ProcessThematicBreakBlock(ThematicBreakBlock thematicBreakBlock)
        {
            _result.Append(new string(thematicBreakBlock.ThematicChar, thematicBreakBlock.ThematicCharCount));
            _result.Append(NewLine2);
        }

        private void ProcessContainerInline(ContainerInline containerInline)
        {
            var linkInline = containerInline as LinkInline;
            var emphasisInline = containerInline as EmphasisInline;

            if (linkInline != null)
            {
                if (linkInline.IsImage)
                {
                    _result.Append('!');
                }

                _result.Append('[');
            }
            else if (emphasisInline != null)
            {
                for (int i = 0; i < emphasisInline.DelimiterCount; i++)
                    _result.Append(emphasisInline.DelimiterChar);
            }
            else if (containerInline is LinkDelimiterInline)
            {
                _result.Append('[');
            }

            foreach (Inline inline2 in containerInline)
            {
                ProcessInline(inline2);
            }

            if (linkInline != null)
            {
                _result.Append("](");
                _result.Append(linkInline.Url);
                _result.Append(')');
            }
            else if (emphasisInline != null)
            {
                for (int i = 0; i < emphasisInline.DelimiterCount; i++)
                    _result.Append(emphasisInline.DelimiterChar);
            }
        }
    }
}