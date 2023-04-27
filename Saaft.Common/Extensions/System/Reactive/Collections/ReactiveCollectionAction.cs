using System.Collections.Generic;

namespace System.Reactive.Collections
{
    public static class ReactiveCollectionAction
    {
        public static ReactiveCollectionAction<T> Clear<T>()
            => new ReactiveCollectionClearAction<T>();

        public static ReactiveCollectionAction<T> Insert<T>(int index, T item)
            => new ReactiveCollectionInsertAction<T>()
            {
                Index = index,
                Item = item
            };

        public static ReactiveCollectionAction<T> Move<T>(int oldIndex, int newIndex)
            => new ReactiveCollectionMoveAction<T>()
            {
                NewIndex = newIndex,
                OldIndex = oldIndex
            };

        public static ReactiveCollectionAction<T> Remove<T>(int index)
            => new ReactiveCollectionRemoveAction<T>()
            {
                Index = index
            };

        public static ReactiveCollectionAction<T> Reset<T>(IReadOnlyList<T> items)
            => new ReactiveCollectionResetAction<T>()
            {
                Items = items
            };
    }

    public abstract class ReactiveCollectionAction<T>
    { }
}
