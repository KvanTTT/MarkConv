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
            return Process(new TextFile(data, ""));
        }

        public virtual string Process(TextFile file)
        {
            var parser = new Parser(Options, Logger);
            var parseResult = parser.Parse(file);
            var checker = new Checker(Logger);
            checker.Check(parseResult);
            var converter = new Converter(Options, Logger);
            return converter.ConvertAndReturn(parseResult);
        }
    }
}
