namespace System.Reactive.Linq
{
    public delegate IObservable<TOut> ReactiveOperation<TIn, TOut>(IObservable<TIn> source);
}
