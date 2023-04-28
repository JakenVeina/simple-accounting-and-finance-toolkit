using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Collections;

namespace System
{
    public static class ObservableExtensions
    {
        public static ReactiveCollection<T> ToReactiveCollection<T>(this IObservable<ReactiveCollectionAction<T>> actions)
            => new(actions);

        public static ReactiveReadOnlyProperty<T?> ToReactiveReadOnlyProperty<T>(this IObservable<T?> source)
            => ReactiveReadOnlyProperty.Create(source);

        public static ReactiveReadOnlyProperty<T> ToReactiveReadOnlyProperty<T>(
                this    IObservable<T>  source,
                        T               initialValue)
            => ReactiveReadOnlyProperty.Create(source, initialValue);
    }
}
