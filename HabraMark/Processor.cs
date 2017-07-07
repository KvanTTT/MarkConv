using System;
using System.Collections.Generic;
using static HabraMark.MarkdownRegex;

namespace HabraMark
{
    public class Processor
    {
        public ILogger Logger { get; set; } = new Logger();

        public ProcessorOptions Options { get; set; }

        public Processor(ProcessorOptions options = null)
            => Options = options ?? new ProcessorOptions();

        public string Process(string original)
        {
            return ProcessAndGetTableOfContents(original).Result;
        }

        public ProcessorResult ProcessAndGetTableOfContents(string original)
        {
            var linesProcessor = new LinesProcessor(Options)
            {
                Logger = Logger
            };
            var linksHtmlProcessor = new LinksHtmlProcessor(Options)
            {
                Logger = Logger
            };

            string[] lines = original.Split(LineBreaks, StringSplitOptions.None);
            LinesProcessorResult linesProcessorResult = linesProcessor.Process(lines);
            string result = string.Join("\n", linesProcessorResult.Lines);
            result = linksHtmlProcessor.Process(result, linesProcessorResult.Headers);
            List<string> tableOfContents = linesProcessor.GenerateTableOfContents(linesProcessorResult);

            return new ProcessorResult(result, tableOfContents);
        }
    }
}
