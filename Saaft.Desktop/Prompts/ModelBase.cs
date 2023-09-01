using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace Saaft.Desktop.Prompts
{
    public abstract class ModelBase<T>
        : DisposableBase,
            IHostedModel
    {
        protected ModelBase()
        {
            _closeRequested = new();
            
            _cancelCommand = ReactiveCommand.Create(_closeRequested);

            _closed = Observable.Create<Unit>(observer => Result
                .Finally(() =>
                {
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                })
                .Subscribe());
        }

        public ReactiveCommand CancelCommand
            => _cancelCommand;

        public IObservable<Unit> Closed
            => _closed;

        public IObserver<Unit> OnCloseRequested
            => _closeRequested;

        public abstract IObservable<T> Result { get; }

        public abstract ReactiveReadOnlyValue<string> Title { get; }

        protected IObservable<Unit> CloseRequested
            => _closeRequested;

        protected override void OnDisposing(DisposalType type)
        {
            _closeRequested.OnCompleted();

            _closeRequested.Dispose();
        }

        private readonly ReactiveCommand    _cancelCommand;
        private readonly IObservable<Unit>  _closed;
        private readonly Subject<Unit>      _closeRequested;
    }
}
