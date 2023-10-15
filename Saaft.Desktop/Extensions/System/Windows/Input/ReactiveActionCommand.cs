using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Windows.Input
{
    public sealed class ReactiveActionCommand
        : ReactiveCommandBase,
            IReactiveActionCommand
    {
        public static ReactiveActionCommand Create(Action onExecuteRequested)
            => new(
                canExecute:         static () => true,
                canExecuteChanged:  Observable.Empty<Unit>(),
                onExecuteRequested: Observer.Create<Unit>(onNext: _ => onExecuteRequested.Invoke()));

        public static ReactiveActionCommand Create(IObserver<Unit> onExecuteRequested)
            => new(
                canExecute:         static () => true,
                canExecuteChanged:  Observable.Empty<Unit>(),
                onExecuteRequested: onExecuteRequested);

        public static ReactiveActionCommand Create(ReactiveOperation<Unit, Unit> executeOperation)
        {
            var executeRequested = new Subject<Unit>();

            var executionSubscription = executeRequested
                .ApplyOperation(executeOperation)
                .Subscribe();

            return new(
                canExecute:         static () => true,
                canExecuteChanged:  Observable.Empty<Unit>(),
                onExecuteRequested: Observer.Create<Unit>(
                    onNext:         executeRequested.OnNext,
                    onError:        executeRequested.OnError,
                    onCompleted:    () =>
                    {
                        executeRequested.OnCompleted();
                        executeRequested.Dispose();
                        executionSubscription.Dispose();
                    }));
        }

        public static ReactiveActionCommand Create(
            IObservable<bool>               canExecute,
            ReactiveOperation<Unit, Unit>   executeOperation)
        {
            var executeRequested = new Subject<Unit>();

            var executionSubscription = executeRequested
                .ApplyOperation(executeOperation)
                .Subscribe();

            var canExecuteState = false;

            return new(
                canExecute:         () => canExecuteState,
                canExecuteChanged:  canExecute
                    .Do(canExecute => canExecuteState = canExecute)
                    .SelectUnit(),
                onExecuteRequested: Observer.Create<Unit>(
                    onNext:         executeRequested.OnNext,
                    onError:        executeRequested.OnError,
                    onCompleted:    () =>
                    {
                        canExecuteState = false;
                        executeRequested.OnCompleted();
                        executeRequested.Dispose();
                        executionSubscription.Dispose();
                    }));
        }

        private ReactiveActionCommand(
                Func<bool>          canExecute,
                IObservable<Unit>   canExecuteChanged,
                IObserver<Unit>     onExecuteRequested)
            : base(canExecuteChanged)
        {
            _canExecute         = canExecute;
            _onExecuteRequested = onExecuteRequested;
        }

        public bool CanExecute
            => _canExecute.Invoke();
        
        public void Execute()
            => _onExecuteRequested.OnNext(Unit.Default);

        protected override bool OnCanExecute(object? parameter)
            => CanExecute;

        protected override void OnExecute(object? parameter)
            => Execute();

        private readonly Func<bool>         _canExecute;
        private readonly IObserver<Unit>    _onExecuteRequested;
    }
}
