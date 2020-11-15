using System;

namespace MarkConv.Nodes
{
    public abstract class Node
    {
        public TextFile File { get; }

        public int Start { get; }

        public int Length { get; }

        public string LineColumnSpan
        {
            get
            {
                File.GetLineColumnFromLinear(Start, out int startLine, out int startColumn);
                File.GetLineColumnFromLinear(Start + Length, out int endLine, out int endColumn);
                return $"[{startLine},{startColumn}..{endLine},{endColumn})";
            }
        }

        public string Substring => File.GetSubstring(Start, Length);

        protected Node(TextFile file, int start, int length)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
            Start = start;
            Length = length;
        }

        public override string ToString() => $"{Substring} at {LineColumnSpan}";
    }
}