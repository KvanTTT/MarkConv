using System;
using System.Collections.Generic;
using System.IO;

namespace MarkConv
{
    public class TextFile
    {
        private readonly int[] _lineIndexes;

        private const int StartLine = 1;

        private const int StartColumn = 1;

        public string Name { get; }

        public string Data { get; }

        public int[] LineIndexes => _lineIndexes;

        public static TextFile Read(string fileName)
        {
            return new TextFile(File.ReadAllText(fileName), fileName);
        }

        public TextFile(string data, string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Data = data ?? throw new ArgumentNullException(nameof(data));

            string text = Data;

            var lineIndexesBuffer = new List<int>(text.Length / 25) { 0 };
            int textIndex = 0;
            while (textIndex < text.Length)
            {
                char c = text[textIndex];
                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && textIndex + 1 < text.Length && text[textIndex + 1] == '\n')
                        textIndex++;

                    lineIndexesBuffer.Add(textIndex + 1);
                }
                textIndex++;
            }

            _lineIndexes = lineIndexesBuffer.ToArray();
        }

        public string RenderToLineColumn(int start, int length)
        {
            GetLineColumnFromLinear(start, out int startLine, out int startColumn);
            GetLineColumnFromLinear(start + length, out int endLine, out int endColumn);

            if (startLine == endLine)
            {
                return startColumn == endColumn
                    ? $"[{startLine},{startColumn})"
                    : $"[{startLine},{startColumn}..{endColumn})";
            }

            return startColumn == endColumn
                ? $"[{startLine}..{endLine},{startColumn})"
                : $"[{startLine},{startColumn}..{endLine},{endColumn})";
        }

        public void GetLineColumnFromLinear(int position, out int line, out int column)
        {
            if (position < 0 || position > Data.Length)
            {
                throw new IndexOutOfRangeException($"linear position {position} out of range for file {Name}");
            }

            line = Array.BinarySearch(_lineIndexes, position);
            if (line < 0)
            {
                line = line == -1 ? 0 : ~line - 1;
            }

            column = position - _lineIndexes[line] + StartColumn;
            line += StartLine;
        }

        public string GetSubstring(int start, int length)
        {
            if (start + length > Data.Length)
                return "";

            return Data.Substring(start, length);
        }
    }
}