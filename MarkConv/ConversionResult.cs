using System;
using System.Text;

namespace MarkConv
{
    public class ConversionResult
    {
        private const string NewLine = "\n";
        private int _currentIndent; // TODO: normalize indents
        private readonly StringBuilder _result;

        public int CurrentColumn { get; private set; }

        public ConversionResult(int capacity = 0)
        {
            _result = new StringBuilder(capacity);
        }

        public void SetIndent(int indent)
        {
            _currentIndent = indent;
        }

        public bool IsLastCharWhitespace() => _result.Length == 0 || char.IsWhiteSpace(_result[^1]);

        public void Append(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;

            AppendIndent();
            _result.Append(str);
            CurrentColumn += str.Length;
        }

        public void Append(ReadOnlySpan<char> str)
        {
            if (str.IsEmpty)
                return;

            AppendIndent();
            _result.Append(str);
            CurrentColumn += str.Length;
        }

        public void Append(char c)
        {
            AppendIndent();
            _result.Append(c);
            CurrentColumn += 1;
        }

        public void Append(char c, int count)
        {
            AppendIndent();
            _result.Append(c, count);
            CurrentColumn += count;
        }

        public void AppendIndent()
        {
            if (_result.Length > 0 && _result[^1] == '\n' && _currentIndent > 0)
            {
                _result.Append(' ', _currentIndent);
                CurrentColumn = _currentIndent;
            }
        }

        public void EnsureNewLine(bool doubleNl = false)
        {
            if (doubleNl)
            {
                if (_result.Length < 2)
                    return;

                if (_result[^1] != '\n')
                {
                    AppendNewLine();
                    AppendNewLine();
                }

                if (_result[^2] != '\n')
                    AppendNewLine();
            }
            else
            {
                if (_result.Length < 1)
                    return;

                if (_result[^1] != '\n')
                    AppendNewLine();
            }
        }

        public void AppendNewLine()
        {
            _result.Append(NewLine);
            CurrentColumn = 0;
        }

        public override string ToString() => _result.ToString();
    }
}