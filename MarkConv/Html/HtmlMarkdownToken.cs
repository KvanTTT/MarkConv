using Antlr4.Runtime;

namespace MarkConv.Html
{
    public abstract class HtmlMarkdownToken : IToken
    {
        public abstract string Text { get; }

        public abstract int Type { get; }

        public int Line => 0;

        public int Column => 0;

        public int Channel => 0;

        public int TokenIndex { get; }

        public int StartIndex { get; }

        public int StopIndex { get; }

        public ITokenSource TokenSource { get; }

        public ICharStream InputStream { get; }

        protected HtmlMarkdownToken(int index, int start, int stop)
        {
            TokenIndex = index;
            StartIndex = start;
            StopIndex = stop;
        }

        public override string ToString()
        {
            string str1 = string.Empty;
            string text = Text;
            string str2 = text == null ? "<no text>" : text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
            string displayName = Type.ToString();
            return "[@" + TokenIndex + "," + StartIndex + ":" + StopIndex + "='" + str2 + "',<" + displayName + ">" + str1 + ":" + Column + "]";
        }
    }
}