using System.Collections.Generic;

namespace HabraMark
{
    public class LinesProcessorResult
    {
        public List<string> Lines { get; }

        public List<Header> Headers { get; }

        public LinesProcessorResult(List<string> lines, List<Header> headers)
        {
            Lines = lines;
            Headers = headers;
        }
    }
}
