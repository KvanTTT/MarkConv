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
            var parser = new Parser(Options, Logger);
            var parseResult = parser.Parse(file);
            var converter = new Converter(Options, Logger);
            var result = converter.ConvertAndReturn(parseResult);
            return new ProcessorResult(result, new List<string> { "TODO" });
        }
    }
}
