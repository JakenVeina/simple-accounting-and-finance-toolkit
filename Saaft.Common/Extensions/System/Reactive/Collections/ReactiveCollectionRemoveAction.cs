namespace System.Reactive.Collections
{
    public sealed class ReactiveCollectionRemoveAction<T>
        : ReactiveCollectionAction<T>
    {
        public required int Index { get; init; }
    }
}
