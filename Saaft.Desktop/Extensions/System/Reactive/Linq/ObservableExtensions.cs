using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Reactive.Linq
{
    public static class ObservableExtensions
    {
        public static IPropertyChangedEventSource ToEventPattern(this IObservable<EventPattern<object?, PropertyChangedEventArgs>> source)
            => new PropertyChangedEventSource(source);

        public static ICollectionChangedEventSource ToEventPattern(this IObservable<EventPattern<object?, NotifyCollectionChangedEventArgs>> source)
            => new CollectionChangedEventSource(source);
    }
}
