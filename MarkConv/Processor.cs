using System.Collections.Generic;

namespace MarkConv
{
    public class Processor
    {
        public ILogger Logger { get; set; } = new Logger();

        public ProcessorOptions Options { get; }

        public Processor(ProcessorOptions options = null) => Options = options ?? new ProcessorOptions();

        public string Process(string data)
        {
            return ProcessAndGetTableOfContents(new TextFile(data, "")).Result;
        }

        public virtual ProcessorResult ProcessAndGetTableOfContents(TextFile file)
        {
            var parser = new HtmlMarkdownParser(Options, Logger);
            var node = parser.ParseHtmlMarkdown(file);
            var converter = new Converter(Options, Logger, parser.EndOfLine);
            var result = converter.ConvertAndReturn(node);
            return new ProcessorResult(result, new List<string> { "TODO" });
        }
    }
}
