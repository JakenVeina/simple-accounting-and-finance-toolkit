using System.Reactive.Linq;

namespace System
{
    public static class ReactiveDisposable
    {
        public static IObservable<TResource> Create<TResource>(Func<TResource> resourceFactory)
                where TResource : IDisposable
            => Observable.Create<TResource>(observer =>
            {
                var resource = resourceFactory.Invoke();

                observer.OnNext(resource);

                return resource;
            });
    }
}
