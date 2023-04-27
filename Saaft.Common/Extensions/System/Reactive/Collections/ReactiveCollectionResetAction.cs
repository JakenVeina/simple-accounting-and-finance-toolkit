using System.Collections.Generic;

namespace System.Reactive.Collections
{
    public sealed class ReactiveCollectionResetAction<T>
        : ReactiveCollectionAction<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
    }
}
