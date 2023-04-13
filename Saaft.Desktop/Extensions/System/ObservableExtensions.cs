using System.ComponentModel;

namespace System
{
    public static class ObservableExtensions
    {
        public static ReactiveProperty<T?> ToReactiveProperty<T>(this IObservable<T?> source)
            => ReactiveProperty.Create(source);
    }
}
