namespace System.Collections.Generic
{
    public static class ReadOnlyListExtensions
    {
        public static IReadOnlyList<T> AsReadOnly<T>(this IReadOnlyList<T> list)
            => list;
    }
}
