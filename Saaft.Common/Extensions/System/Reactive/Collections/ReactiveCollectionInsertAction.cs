namespace System.Reactive.Collections
{
    public sealed class ReactiveCollectionInsertAction<T>
        : ReactiveCollectionAction<T>
    {
        public required int Index { get; init; }

        public required T Item { get; init; }
    }
}
