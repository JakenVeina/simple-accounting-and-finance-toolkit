using System.Collections.Generic;

namespace System.Linq
{
    public static class EnumerableExtensions
    {
        public static IOrderedEnumerable<T> ApplyOrderByClause<T>(
                this    IEnumerable<T>                              source,
                        Func<IEnumerable<T>, IOrderedEnumerable<T>> clause)
            => clause.Invoke(source);

        public static int IndexOf<T>(
                this    IEnumerable<T>  source,
                        T               item)
            => source
                    .Select(static (sourceItem, index) => (sourceItem, index))
                    .Where(@params => EqualityComparer<T>.Default.Equals(@params.sourceItem, item))
                    .Select(static @params => @params.index.ToNullable())
                    .FirstOrDefault()
                ?? -1;
    }
}
