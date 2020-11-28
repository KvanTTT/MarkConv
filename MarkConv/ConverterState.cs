using System.Collections.Generic;

namespace MarkConv
{
    internal class ConverterState
    {
        private Dictionary<string, Anchor> NewAnchors { get; }

        public HeaderToLinkConverter HeaderToLinkConverter { get; }

        public int HeadingNumber { get; set; } = -1;

        public int ImageLinkNumber { get; set; } = -1;

        public ConverterState()
        {
            NewAnchors = new Dictionary<string, Anchor>();
            HeaderToLinkConverter = new HeaderToLinkConverter(NewAnchors);
        }
    }
}