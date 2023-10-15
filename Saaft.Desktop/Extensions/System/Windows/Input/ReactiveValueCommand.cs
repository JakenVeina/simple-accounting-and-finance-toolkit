using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Windows.Input
{
    public sealed class ReactiveValueCommand<T>
            : ReactiveCommandBase,
                IReactiveValueCommand<T>
        where T : struct
    {
        public static ReactiveValueCommand<T> Create(ReactiveOperation<T, Unit>  executeOperation)
            => Create<Unit>(
                canExecuteState:    Observable.Empty<Unit>(),
                canExecute:         static (_, _) => true,
                executeOperation:   executeOperation);

        public static ReactiveValueCommand<T> Create<TState>(
            IObservable<TState>         canExecuteState,
            Func<TState, T, bool>       canExecute,
            ReactiveOperation<T, Unit>  executeOperation)
        {
            var executeRequested = new Subject<T>();

            var executionSubscription = executeRequested
                .ApplyOperation(executeOperation)
                .Subscribe();

            var isActive = false;
            var latestState = default(TState)!;

            return new(
                canExecute:         parameter => isActive
                    && ((parameter is null)
                        || canExecute.Invoke(latestState, parameter.Value)),
                canExecuteChanged:  canExecuteState
                    .Do(state =>
                    {
                        latestState = state;
                        isActive = true;
                    })
                    .SelectUnit(),
                onExecuteRequested: Observer.Create<T>(
                    onNext:         executeRequested.OnNext,
                    onError:        executeRequested.OnError,
                    onCompleted:    () =>
                    {
                        isActive = false;
                        executeRequested.OnCompleted();
                        executeRequested.Dispose();
                        executionSubscription.Dispose();
                    }));
        }

        private ReactiveValueCommand(
                Predicate<T?>       canExecute,
                IObservable<Unit>   canExecuteChanged,
                IObserver<T>        onExecuteRequested)
            : base(canExecuteChanged)
        {
            _canExecute         = canExecute;
            _onExecuteRequested = onExecuteRequested;
        }

        public bool CanExecute(T? parameter)
            => _canExecute.Invoke(parameter);

        public void Execute(T parameter)
            => _onExecuteRequested.OnNext(parameter);

        protected override bool OnCanExecute(object? parameter)
            => parameter switch
            {
                null    => CanExecute(default),
                T value => CanExecute(value),
                _       => false
            };

        protected override void OnDisposing(DisposalType type)
        {
            base.OnDisposing(type);

            if (type is DisposalType.Managed)
                _onExecuteRequested.OnCompleted();
        }

        protected override void OnExecute(object? parameter)
        {
            switch(parameter)
            {
                case null when typeof(T) == typeof(Unit):
                    Execute(default);
                    break;

                case null:
                    throw new ArgumentNullException(nameof(parameter));

                case T value:
                    Execute(value);
                    break;

                default:
                    ThrowUnsupportedParameterType(parameter.GetType(), typeof(T), nameof(parameter));
                    break;
            }
        }

        private readonly Predicate<T?>  _canExecute;
        private readonly IObserver<T>   _onExecuteRequested;
    }
}
