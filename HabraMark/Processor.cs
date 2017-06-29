using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            List<string> lines = original.Split(LineBreaks, StringSplitOptions.None).ToList();

            var linesProcessor = new LinesProcessor(Options);
            var linksHtmlProcessor = new LinksHtmlProcessor(Options);

            List<Header> headers = linesProcessor.Process(lines);
            string result = string.Join("\n", lines);
            result = linksHtmlProcessor.Process(result, headers);

            return result;
        }
    }
}
