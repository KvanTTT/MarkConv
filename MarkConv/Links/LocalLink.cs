using MarkConv.Nodes;

namespace MarkConv.Links
{
    public class LocalLink : Link
    {
        public LocalLink(Node node, string address, bool isImage = false, int start = -1, int length = -1)
            : base(node, address, isImage, start, length)
        {
        }
    }
}