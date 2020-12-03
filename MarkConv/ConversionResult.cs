using System;
using System.Text;

namespace MarkConv
{
    public class ConversionResult
    {
        private int _currentIndent; // TODO: normalize indents
        private readonly StringBuilder _result;

        public string EndOfLine { get; }

        public int CurrentColumn { get; private set; }

        public ConversionResult(string endOfLine = "\n", int capacity = 0)
        {
            EndOfLine = endOfLine;
            _result = new StringBuilder(capacity);
        }

        public void SetIndent(int indent)
        {
            _currentIndent = indent;
        }

        public bool IsLastCharWhitespaceOrLeadingPunctuation()
        {
            if (_result.Length == 0)
                return true;

            var lastChar = _result[^1];
            return char.IsWhiteSpace(lastChar) || lastChar == '(' || lastChar == '[' || lastChar == '{';
        }

        public void Append(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;

            AppendIndentIfRequired();
            _result.Append(str);
            CurrentColumn += str.Length;
        }

        public void Append(ReadOnlySpan<char> str)
        {
            if (str.IsEmpty)
                return;

            AppendIndentIfRequired();
            _result.Append(str);
            CurrentColumn += str.Length;
        }

        public void Append(char c)
        {
            AppendIndentIfRequired();
            _result.Append(c);
            CurrentColumn += 1;
        }

        public void Append(char c, int count)
        {
            AppendIndentIfRequired();
            _result.Append(c, count);
            CurrentColumn += count;
        }

        private void AppendIndentIfRequired()
        {
            if (_result.Length > 0 && _result[^1] == '\n' && _currentIndent > 0)
            {
                _result.Append(' ', _currentIndent);
                CurrentColumn = _currentIndent;
            }
        }

        public void EnsureNewLine(bool doubleNl = false)
        {
            int endOfLineLength = EndOfLine.Length;

            if (doubleNl)
            {
                if (_result.Length < 2 * endOfLineLength)
                    return;

                if (_result[^1] != '\n')
                {
                    AppendNewLine();
                    AppendNewLine();
                    return;
                }

                if (_result[^(endOfLineLength == 1 ? 2 : 3)] != '\n')
                    AppendNewLine();
            }
            else
            {
                if (_result.Length < 1 * endOfLineLength)
                    return;

                if (_result[^1] != '\n')
                    AppendNewLine();
            }
        }

        public void AppendNewLine()
        {
            TrimEndSpaces();
            _result.Append(EndOfLine);
            CurrentColumn = 0;
        }

        private void TrimEndSpaces()
        {
            if (_result.Length == 0)
                return;

            int index = _result.Length - 1;
            while (IsWhiteSpace(_result[index]))
                index--;

            _result.Remove(index + 1, _result.Length - index - 1);
        }

        private bool IsWhiteSpace(char c) => c == ' ' || c == '\t';

        public override string ToString() => _result.ToString();
    }
}