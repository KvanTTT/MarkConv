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
            new Regex(@"(\s*)" + MarkdownBlockMarker + @"(\d+)" + @"(\s*)", RegexOptions.Compiled);

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
                    htmlData.Append(index.ToString());
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
                var matches = MarkdownBlockRegex.Matches(htmlTextNode.Text);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        var groups = match.Groups;
                        int blockNumber = int.Parse(groups[2].Value);
                        var markdownBlock = _container[blockNumber];

                        if (_container is ListItemBlock && blockNumber == 0)
                        {
                            _result.Append(' ', markdownBlock.Column - _result.CurrentColumn);
                        }
                        else
                        {
                            _result.Append(groups[1].Value);
                            _result.EnsureNewLine(true);
                        }

                        var markdownConverter = new MarkdigConverter(Options, Logger, _result);
                        markdownConverter.ConvertBlock(markdownBlock);
                        _result.Append(groups[3].Value);
                        _lastBlockIsMarkdown = true;
                    }
                }
                else
                {
                    _result.Append(htmlTextNode.Text);
                    _lastBlockIsMarkdown = false;
                }

                return;
            }

            if (htmlNode.Name != "#document")
            {
                if (_lastBlockIsMarkdown)
                    _result.EnsureNewLine(true);

                _result.Append('<');

                if (closing)
                {
                    _result.Append('/');
                }

                _result.Append(htmlNode.Name);

                foreach (HtmlAttribute htmlAttribute in htmlNode.Attributes)
                {
                    _result.Append(' ');
                    char quote = htmlAttribute.QuoteType == AttributeValueQuote.SingleQuote ? '\'' : '"';

                    _result.Append(htmlAttribute.Name);
                    _result.Append('=');

                    _result.Append(quote);
                    _result.Append(htmlAttribute.Value);
                    _result.Append(quote);
                }

                if (htmlNode.EndNode == htmlNode)
                {
                    _result.Append('/');
                }

                _result.Append('>');
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
    }
}