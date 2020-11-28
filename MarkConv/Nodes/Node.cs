using System;

namespace MarkConv.Nodes
{
    public abstract class Node
    {
        public TextFile File { get; }

        public int Start { get; }

        public int Length { get; }

        public string LineColumnSpan => File.RenderToLineColumn(Start, Length);

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