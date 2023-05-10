using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace Saaft.Desktop.Prompts
{
    public abstract class ModelBase
        : HostedModelBase
    {
        protected ModelBase()
        {
            _cancelCommandExecuted = new();
            _cancelCommand = ReactiveCommand.Create(_cancelCommandExecuted);
        }

        public ReactiveCommand CancelCommand
            => _cancelCommand;

        public abstract IObservable<Unit> Completed { get; }

        protected override void OnDisposing(DisposalType type)
        {
            _cancelCommandExecuted.OnCompleted();
            _cancelCommandExecuted.Dispose();
        }

        private readonly ReactiveCommand    _cancelCommand;
        private readonly Subject<Unit>      _cancelCommandExecuted;
    }

    public abstract class ModelBase<T>
        : ModelBase
    {
        protected ModelBase()
            => _completed = Observable.Create<Unit>(observer => Result
                .Finally(() =>
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                })
                .Subscribe());

        public sealed override IObservable<Unit> Completed
            => _completed;

        public abstract IObservable<T> Result { get; }

        private readonly IObservable<Unit> _completed;
    }
}
