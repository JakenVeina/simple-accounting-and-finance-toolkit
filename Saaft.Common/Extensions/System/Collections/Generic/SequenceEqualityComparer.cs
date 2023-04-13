using System;
using System.Linq;

namespace System.Collections.Generic
{
    public class SequenceEqualityComparer<T>
        : IEqualityComparer<IEnumerable<T>>
    {
        public static readonly SequenceEqualityComparer<T> Default
            = new();

        public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
            => (x == y)
                || (    (x is not null)
                    &&  (y is not null)
                    &&  x.SequenceEqual(y));

        public int GetHashCode(IEnumerable<T> obj)
        {
            var builder = new HashCode();
            foreach(var item in obj)
                builder.Add(item);
            return builder.ToHashCode();
        }
    }
}
