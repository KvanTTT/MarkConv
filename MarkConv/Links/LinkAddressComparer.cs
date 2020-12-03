using System.Collections.Generic;

namespace MarkConv.Links
{
    public class LinkAddressComparer : IEqualityComparer<Link>
    {
        public static readonly LinkAddressComparer Instance = new LinkAddressComparer();

        public bool Equals(Link? x, Link? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (ReferenceEquals(x, null))
                return false;

            if (ReferenceEquals(y, null))
                return false;

            if (ReferenceEquals(x, y))
                return true;

            if (x.GetType() != y.GetType())
                return false;

            return x.Address.Equals(y.Address);
        }

        public int GetHashCode(Link link) => link.Address.GetHashCode();
    }
}