using System;

[assembly: CLSCompliant(false)]

namespace MarkConv
{
    public class Processor
    {
        public ILogger Logger { get; }

        public ProcessorOptions Options { get; }

        public Processor(ProcessorOptions options, ILogger logger)
        {
            Options = options ?? new ProcessorOptions();
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Process(string data) => Process(new TextFile(data, ""));

        public string Process(TextFile file)
        {
            var parser = new Parser(Options, Logger);
            var parseResult = parser.Parse(file);
            var checker = new Checker(Options, Logger);
            checker.Check(parseResult);
            var converter = new Converter(Options, Logger);
            var result = converter.ConvertAndReturn(parseResult);
            var postprocessor = new Postprocessor(Options, Logger);
            postprocessor.Postprocess(result);
            return result;
        }
    }
}
