using System.Collections.Generic;

namespace MarkConv
{
    public class ProcessorResult
    {
        public string Result { get; }

        public List<string> TableOfContents { get; }

        public ProcessorResult(string result, List<string> tableOfContents)
        {
            Result = result;
            TableOfContents = tableOfContents;
        }
    }
}
