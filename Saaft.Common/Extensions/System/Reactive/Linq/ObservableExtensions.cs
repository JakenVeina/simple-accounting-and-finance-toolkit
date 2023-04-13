namespace System.Reactive.Linq
{
    public static class ObservableExtensions
    {
        public static IObservable<T> ShareReplay<T>(this IObservable<T> source, int bufferSize)
            => source
                .Replay(bufferSize)
                .RefCount();
    }
}
