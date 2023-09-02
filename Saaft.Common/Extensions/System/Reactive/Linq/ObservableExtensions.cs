using System.Reactive.Subjects;

namespace System.Reactive.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<TOut> ApplyOperation<TIn, TOut>(
                this    IObservable<TIn>                source,
                        ReactiveOperation<TIn, TOut>    operation)
            => operation.Invoke(source);

        public static IObservable<T> OnSubscribed<T>(
                this IObservable<T> source,
                Action              onSubscribed)
            => Observable.Create<T>(observer =>
            {
                var subscription = source.Subscribe(observer);

                onSubscribed.Invoke();

                return subscription;
            });

        public static IObservable<Unit> SelectUnit<T>(this IObservable<T> source)
            => source.Select(static _ => Unit.Default);

        public static IObservable<T> Share<T>(this IObservable<T> source)
            => source
                .Publish()
                .RefCount();

        public static IObservable<T> ShareReplay<T>(this IObservable<T> source, int bufferSize)
            => source
                .Multicast(new ResettingReplaySubject<T>(bufferSize))
                .RefCount();

        public static IBasicEventSource ToEventPattern(
                this    IObservable<Unit>   source,
                        object?             sender)
            => BasicEventSource.Create(
                sender: sender,
                source: source);

        public static IObservable<T> WhereNotNull<T>(this IObservable<T?> source)
                where T : class
            => source.Where(static value => value is not null)!;
    }
}
