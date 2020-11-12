using MarkConv.Nodes;

namespace MarkConv.Links
{
    public class RelativeLink : Link
    {
        public RelativeLink(Node node, string address, int start = -1, int length = -1)
            : base(node, address, false, start, length)
        {
        }
    }
}
