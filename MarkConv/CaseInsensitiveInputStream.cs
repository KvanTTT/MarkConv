using Antlr4.Runtime;

namespace MarkConv
{
    public class CaseInsensitiveInputStream : AntlrInputStream
    {
        private readonly string _lookaheadData;

        public CaseInsensitiveInputStream(string input)
            : base(input)
        {
            _lookaheadData = input.ToLowerInvariant();
        }

        public override int La(int i)
        {
            if (i == 0)
            {
                return 0; // undefined
            }

            if (i < 0)
            {
                i++; // e.g., translate LA(-1) to use offset i=0; then data[p+0-1]
                if (p + i - 1 < 0)
                {
                    return IntStreamConstants.Eof; // invalid; no char before first char
                }
            }

            int index = p + i - 1;

            if (index >= n)
            {
                return IntStreamConstants.Eof;
            }

            return _lookaheadData[index];
        }
    }
}

