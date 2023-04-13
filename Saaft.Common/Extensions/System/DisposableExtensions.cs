using System.Reactive.Disposables;

namespace System
{
    public static class DisposableExtensions
    {
        public static TDisposable DisposeWith<TDisposable>(
                this TDisposable    disposable,
                CompositeDisposable disposal)
            where TDisposable : IDisposable
        {
            disposal.Add(disposable);
            return disposable;
        }
    }
}
