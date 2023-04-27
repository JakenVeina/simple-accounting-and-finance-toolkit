using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Collections;

namespace System
{
    public static class ObservableExtensions
    {
        public static ReactiveCollection<T> ToReactiveCollection<T>(this IObservable<ReactiveCollectionAction<T>> actions)
            => new(actions);

        public static ReactiveProperty<T?> ToReactiveProperty<T>(this IObservable<T?> source)
            => ReactiveProperty.Create(source);

        public static ReactiveProperty<T> ToReactiveProperty<T>(
                this    IObservable<T>  source,
                        T               initialValue)
            => ReactiveProperty.Create(source, initialValue);
    }
}
