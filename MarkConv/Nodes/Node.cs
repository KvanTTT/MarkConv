namespace MarkConv.Nodes
{
    public abstract class Node
    {
        public Node Parent { get; set; }

        public int Start { get; }

        public int Length { get; }

        public Node(int start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}