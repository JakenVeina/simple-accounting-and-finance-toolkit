using System.Reactive.Subjects;

namespace System.Reactive.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<TOut> ApplyOperation<TIn, TOut>(
                this    IObservable<TIn>                            source,
                        Func<IObservable<TIn>, IObservable<TOut>>   operation)
            => operation.Invoke(source);

        public static IObservable<T> Share<T>(this IObservable<T> source)
            => source
                .Publish()
                .RefCount();

        public static IObservable<T> ShareReplay<T>(this IObservable<T> source, int bufferSize)
            => source
                .Multicast(new ResettingReplaySubject<T>(bufferSize))
                .RefCount();

        public static IObservable<T> WhereNotNull<T>(this IObservable<T?> source)
                where T : class
            => source.Where(value => value is not null)!;
    }
}
