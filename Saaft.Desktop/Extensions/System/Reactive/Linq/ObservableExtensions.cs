using System.ComponentModel;

namespace System.Reactive.Linq
{
    public static class ObservableExtensions
    {
        public static IPropertyChangedEventSource ToEventPattern(this IObservable<EventPattern<object?, PropertyChangedEventArgs>> source)
            => new PropertyChangedEventSource(source);
    }
}
