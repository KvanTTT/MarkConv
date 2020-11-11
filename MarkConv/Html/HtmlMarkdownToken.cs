using System;
using Antlr4.Runtime;

namespace MarkConv.Html
{
    public abstract class HtmlMarkdownToken : IToken
    {
        public TextFile File { get; }

        public abstract string Text { get; }

        public abstract int Type { get; }

        public int Line
        {
            get
            {
                File.GetLineColumnFromLinear(StartIndex, out int line, out _);
                return line;
            }
        }

        public int Column
        {
            get
            {
                File.GetLineColumnFromLinear(StartIndex, out _, out int column);
                return column;
            }
        }

        public int Channel => 0;

        public int TokenIndex { get; }

        public int StartIndex { get; }

        public int StopIndex { get; }

        public ITokenSource TokenSource { get; }

        public ICharStream InputStream { get; }

        protected HtmlMarkdownToken(TextFile file, int index, int start, int stop)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
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