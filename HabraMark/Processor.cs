using System;
using System.Collections.Generic;
using System.Linq;
using static HabraMark.MdRegex;

namespace HabraMark
{
    public class Processor
    {
        public ProcessorOptions Options { get; set; }

        public Processor(ProcessorOptions options = null)
            => Options = options ?? new ProcessorOptions();

        public string Process(string original)
        {
            var linesProcessor = new LinesProcessor(Options);
            var linksHtmlProcessor = new LinksHtmlProcessor(Options);

            string[] lines = original.Split(LineBreaks, StringSplitOptions.None);
            LinesProcessorResult linesProcessorResult = linesProcessor.Process(lines);
            string result = string.Join("\n", linesProcessorResult.Lines);
            result = linksHtmlProcessor.Process(result, linesProcessorResult.Headers);

            return result;
        }
    }
}
