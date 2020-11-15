using System;

namespace MarkConv
{
    public class Image
    {
        public string Address { get; }

        public Image(string address)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
        }

        public override string ToString() => Address;
    }
}
