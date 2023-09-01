using System;
using System.ComponentModel;
using System.Reactive;

namespace Saaft.Desktop
{
    public interface IHostedModel
    {
        IObservable<Unit> Closed { get; }

        IObserver<Unit> OnCloseRequested { get; }

        ReactiveReadOnlyValue<string> Title { get; }
    }
}
