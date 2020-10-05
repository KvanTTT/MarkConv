using System;
using System.IO;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkdigParserTest
{
    static class Program
    {
        private static string nl = "\n";
        private static string nl2 = "\n\n";

        static void Main(string[] args)
        {
            string fileOrDirectory = args[0];

            string[] files = Directory.Exists(fileOrDirectory)
                ? Directory.GetFiles(fileOrDirectory, "*.md", SearchOption.AllDirectories)
                : new[] {fileOrDirectory};

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            foreach (string file in files)
            {
                var result = ProcessFile(file);
                File.WriteAllText(Path.Combine(baseDirectory, Path.GetFileName(file)), result);
            }
        }

        private static string ProcessFile(string fileName)
        {
            var origin = File.ReadAllText(fileName);

            MarkdownDocument doc = Markdown.Parse(origin);

            StringBuilder result = new StringBuilder(origin.Length);

            foreach (Block child in doc)
            {
                ProcessBlock(result, child);
            }

            // Trim whitespaces and newlines
            int lastWsIndex = result.Length - 1;
            while (lastWsIndex >= 0)
            {
                if (!char.IsWhiteSpace(result[lastWsIndex]))
                    break;
                lastWsIndex--;
            }

            lastWsIndex++;
            if (lastWsIndex < result.Length)
                result.Remove(lastWsIndex, result.Length - lastWsIndex);

            return result.ToString();
        }

        private static void ProcessBlock(StringBuilder result, Block block, bool singleNewLineAfterParagraph = false)
        {
            switch (block)
            {
                case ParagraphBlock paragraphBlock:
                    //result.Append(new string(' ', paragraphBlock.Column));
                    ProcessInline(result, paragraphBlock.Inline);
                    result.Append(singleNewLineAfterParagraph ? nl : nl2);
                    break;

                case QuoteBlock quoteBlock:
                    foreach (Block childQuoteBlock in quoteBlock)
                    {
                        result.Append(quoteBlock.QuoteChar);
                        result.Append(' ');
                        ProcessBlock(result, childQuoteBlock);
                    }
                    break;

                case ListBlock listBlock:
                    foreach (Block childListBlock in listBlock)
                    {
                        if (childListBlock is ListItemBlock listItemBlock)
                        {
                            result.Append(new string(' ', listItemBlock.Column));

                            int columnWidth = listItemBlock.ColumnWidth;
                            int appendWidth;
                            if (listBlock.IsOrdered)
                            {
                                string orderString = listItemBlock.Order.ToString();
                                result.Append(orderString);
                                result.Append(listBlock.OrderedDelimiter);
                                appendWidth = orderString.Length + 1;
                            }
                            else
                            {
                                result.Append(listBlock.BulletType);
                                appendWidth = 1;
                            }
                            result.Append(' ', columnWidth - appendWidth);

                            foreach (var itemBlock in listItemBlock)
                            {
                                // TODO: correct indentation
                                ProcessBlock(result, itemBlock, true);
                            }
                        }
                        else
                        {
                        }
                    }
                    result.Append(nl);
                    break;

                case HeadingBlock headingBlock:
                    result.Append(new string(headingBlock.HeaderChar, headingBlock.Level));
                    result.Append(' ');
                    ProcessInline(result, headingBlock.Inline);
                    result.Append(nl2);
                    break;

                case HtmlBlock htmlBlock:
                    for (int i = 0; i < htmlBlock.Lines.Count; i++)
                    {
                        result.Append(htmlBlock.Lines.Lines[i].ToString());
                        result.Append(nl);
                    }

                    result.Append(nl);
                    break;

                case CodeBlock codeBlock:
                    FencedCodeBlock fencedCodeBlock = codeBlock as FencedCodeBlock;
                    string fencedString = null;

                    if (fencedCodeBlock != null)
                    {
                        fencedString = new string(fencedCodeBlock.FencedChar, fencedCodeBlock.FencedCharCount);
                        result.Append(fencedString);
                        result.Append(' ');
                        result.Append(fencedCodeBlock.Info);
                        result.Append(nl);
                    }

                    for (int i = 0; i < codeBlock.Lines.Count; i++)
                    {
                        if (fencedCodeBlock == null)
                        {
                            result.Append("    ");
                        }
                        result.Append(codeBlock.Lines.Lines[i].ToString());
                        result.Append(nl);
                    }

                    if (fencedCodeBlock != null)
                    {
                        result.Append(fencedString);
                        result.Append(nl2);
                    }
                    break;

                case ThematicBreakBlock thematicBreakBlock:
                    result.Append(new string(thematicBreakBlock.ThematicChar, thematicBreakBlock.ThematicCharCount));
                    result.Append(nl2);
                    break;

                default:
                    throw new NotImplementedException($"Processing of Block type '{block.GetType()}' is not implemented");
            }
        }

        private static void ProcessInline(StringBuilder result, Inline inline)
        {
            switch (inline)
            {
                case LiteralInline literalInline:
                    result.Append(literalInline);
                    break;

                case LineBreakInline _:
                    result.Append(nl);
                    break;

                case CodeInline codeInline:
                    result.Append(codeInline.Delimiter);
                    result.Append(codeInline.Content);
                    result.Append(codeInline.Delimiter);
                    break;

                case ContainerInline containerInline:
                    var linkInline = containerInline as LinkInline;
                    var emphasisInline = containerInline as EmphasisInline;

                    if (linkInline != null)
                    {
                        if (linkInline.IsImage)
                        {
                            result.Append('!');
                        }

                        result.Append('[');
                    }
                    else if (emphasisInline != null)
                    {
                        for (int i = 0; i < emphasisInline.DelimiterCount; i++)
                            result.Append(emphasisInline.DelimiterChar);
                    }
                    else if (containerInline is LinkDelimiterInline)
                    {
                        result.Append('[');
                    }

                    foreach (Inline inline2 in containerInline)
                    {
                        ProcessInline(result, inline2);
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

                    break;

                case AutolinkInline autolinkInline:
                    result.Append('<');
                    result.Append(autolinkInline.Url);
                    result.Append('>');
                    break;

                default:
                    throw new NotImplementedException($"Processing of Inline type '{inline.GetType()}' is not implemented");
            }
        }
    }
}