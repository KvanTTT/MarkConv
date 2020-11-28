using System;

namespace MarkConv
{
    public class Processor
    {
        private readonly ILogger _logger;
        private readonly ProcessorOptions _options;

        public Processor(ProcessorOptions options, ILogger logger)
        {
            _options = options ?? new ProcessorOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Process(string data) => Process(new TextFile(data, ""));

        public string Process(TextFile file)
        {
            var parser = new Parser(_options, _logger, file);
            var parseResult = parser.Parse();
            var checker = new Checker(_options, _logger);
            checker.Check(parseResult);
            var converter = new Converter(_options, _logger, parseResult, new ConverterState());
            var result = converter.ConvertAndReturn();
            var postprocessor = new Postprocessor(_options, _logger);
            postprocessor.Postprocess(result);
            return result;
        }
    }
}
