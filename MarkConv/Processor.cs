using System.Collections.Generic;

namespace MarkConv
{
    public class Processor
    {
        public ILogger Logger { get; set; } = new Logger();

        public ProcessorOptions Options { get; }

        public Processor(ProcessorOptions options = null) => Options = options ?? new ProcessorOptions();

        public string Process(string original)
        {
            return ProcessAndGetTableOfContents(original).Result;
        }

        public virtual ProcessorResult ProcessAndGetTableOfContents(string original)
        {
            var converter = new Converter(Options, Logger);
            var result = converter.Convert(original);
            return new ProcessorResult(result, new List<string> { "TODO"});
        }
    }
}
