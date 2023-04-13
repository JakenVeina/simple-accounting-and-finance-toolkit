namespace System.Collections.Generic
{
    public static class ReadOnlyListExtensions
    {
        public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> list)
            => list;
    }
}
