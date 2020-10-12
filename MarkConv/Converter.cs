using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Markdig;
using Markdig.Syntax;

namespace MarkConv
{
    public class Converter
    {
        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        public const string MarkdownBlockMarker = "markdown_block:";

        private static readonly Regex MarkdownBlockRegex =
            new Regex(MarkdownBlockMarker + @"(\d+)", RegexOptions.Compiled);

        private bool _lastBlockIsMarkdown;

        private ContainerBlock _container;

        private readonly ConversionResult _result;

        public Converter(ProcessorOptions options, ILogger logger, ConversionResult result = null)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
            _result = result ?? new ConversionResult();
        }

        public string Convert(string content)
        {
            MarkdownDocument document = Markdown.Parse(content);
            ConvertContainerBlock(document, content.Length);
            return _result.ToString();
        }

        public void ConvertContainerBlock(ContainerBlock containerBlock, int capacity = 0)
        {
            _container = containerBlock;

            if (_container.All(block => !(block is HtmlBlock)))
            {
                var markdownConverter = new MarkdigConverter(Options, Logger, _result);
                markdownConverter.ConvertBlock(_container);
                return;
            }

            var htmlData = new StringBuilder(capacity);

            for (var index = 0; index < _container.Count; index++)
            {
                Block child = _container[index];
                if (child is HtmlBlock htmlBlock)
                {
                    AppendHtmlData(htmlData, htmlBlock);
                }
                else
                {
                    htmlData.Append(MarkdownBlockMarker);
                    htmlData.Append(index);
                }
            }

            ConvertHtml(htmlData.ToString());
        }

        public string ConvertHtmlAndReturn(string html)
        {
            ConvertHtml(html);
            return _result.ToString();
        }

        private void ConvertHtml(string htmlData)
        {
            var doc = new HtmlDocument();
            using var stringReader = new StringReader(htmlData);
            doc.Load(stringReader);

            Convert(doc.DocumentNode);
        }

        private void AppendHtmlData(StringBuilder htmlData, HtmlBlock htmlBlock)
        {
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
                htmlData.Append(origSpan.Slice(slice.Start, slice.Length).TrimStart());
                htmlData.Append("\n");
            }
        }

        private void Convert(HtmlNode htmlNode, bool closing = false)
        {
            if (htmlNode == null)
            {
                _lastBlockIsMarkdown = false;
                return;
            }

            if (htmlNode is HtmlTextNode htmlTextNode)
            {
                ConvertHtmlTextNode(htmlTextNode);
                return;
            }

            if (htmlNode.Name != "#document")
            {
                if (!ConvertHtmlElement(htmlNode, closing))
                {
                    _lastBlockIsMarkdown = false;
                    return;
                }
            }

            foreach (HtmlNode childNode in htmlNode.ChildNodes)
            {
                Convert(childNode);
            }

            if (htmlNode.Name != "#document" && htmlNode.EndNode != htmlNode)
            {
                Convert(htmlNode.EndNode, true);
            }

            _lastBlockIsMarkdown = false;
        }

        private void ConvertHtmlTextNode(HtmlTextNode htmlTextNode)
        {
            Match match;
            int index = 0;
            int length = htmlTextNode.Text.Length;
            var textSpan = htmlTextNode.Text.AsSpan();
            _lastBlockIsMarkdown = false;

            while ((match = MarkdownBlockRegex.Match(htmlTextNode.Text, index, length)).Success)
            {
                _result.Append(textSpan.Slice(index, match.Index - index));

                int blockNumber = int.Parse(match.Groups[1].Value);
                var markdownBlock = _container[blockNumber];

                if (_container is ListItemBlock && blockNumber == 0)
                    _result.Append(' ', markdownBlock.Column - _result.CurrentColumn);
                else
                    _result.EnsureNewLine(true);

                var markdownConverter = new MarkdigConverter(Options, Logger, _result);
                markdownConverter.ConvertBlock(markdownBlock);
                _lastBlockIsMarkdown = true;

                index = match.Index + match.Length;
                length = htmlTextNode.Text.Length - index;
            }

            var span = textSpan.Slice(match.Index, length - match.Index);
            if (!span.IsEmpty)
            {
                _lastBlockIsMarkdown = false;
                _result.Append(span);
            }
        }

        private bool ConvertHtmlElement(HtmlNode htmlNode, bool closing)
        {
            string name = htmlNode.Name;
            (string, string) additionalAttr = default;

            if (Options.InputMarkdownType == MarkdownType.GitHub)
            {
                if (Options.OutputMarkdownType == MarkdownType.Habr)
                {
                    if (name == "details")
                    {
                        name = "spoiler";
                        var summaryNode = htmlNode.ChildNodes["summary"];
                        if (summaryNode != null)
                        {
                            additionalAttr = ("title", summaryNode.InnerText);
                        }
                    }
                    else if (name == "summary")
                    {
                        return false;
                    }
                }
                else if (Options.OutputMarkdownType == MarkdownType.Dev)
                {
                    if (name == "details")
                    {
                        _result.EnsureNewLine();
                        if (!closing)
                        {
                            _result.Append("{% details");
                            var summaryNode = htmlNode.ChildNodes["summary"];
                            if (summaryNode != null)
                            {
                                _result.Append(" ");
                                _result.Append(summaryNode.InnerText);
                            }

                            _result.Append(" %}");
                        }
                        else
                        {
                            _result.Append("{% enddetails %}");
                        }
                        return true;
                    }

                    if (name == "summary")
                    {
                        return false;
                    }
                }
            }

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

            if (additionalAttr != default)
            {
                ConvertAttribute(additionalAttr.Item1, additionalAttr.Item2, AttributeValueQuote.DoubleQuote);
            }

            if (htmlNode.EndNode == htmlNode)
            {
                _result.Append('/');
            }

            _result.Append('>');

            return true;
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
    }
}