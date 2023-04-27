namespace System.Reactive.Collections
{
    public sealed class ReactiveCollectionMoveAction<T>
        : ReactiveCollectionAction<T>
    {
        public required int NewIndex { get; init; }

        public required int OldIndex { get; init; }
    }
}
