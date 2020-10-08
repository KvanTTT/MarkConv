using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace MarkConv
{
    public class HtmlConverter
    {
        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        private StringBuilder _result;

        public HtmlConverter(ProcessorOptions options = null, ILogger logger = null)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger;
        }

        public string Convert(string html)
        {
            _result = new StringBuilder();

            var doc = new HtmlDocument();
            using var stringReader = new StringReader(html);
            doc.Load(stringReader);

            Convert(doc.DocumentNode);

            var result = _result.ToString();
            if (string.IsNullOrWhiteSpace(result))
                result = html;

            return result;
        }

        private void Convert(HtmlNode htmlNode, bool closing = false)
        {
            if (htmlNode == null)
            {
                return;
            }

            if (htmlNode is HtmlTextNode htmlTextNode)
            {
                _result.Append(htmlTextNode.Text);
                return;
            }

            if (htmlNode.Name != "#document")
            {
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
        }
    }
}