using System.Collections.Generic;

namespace MarkConv.Nodes
{
    public abstract class Node
    {
        public int Start { get; }

        public int Length { get; }

        public List<Node> Children { get; protected set; }

        protected Node(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}