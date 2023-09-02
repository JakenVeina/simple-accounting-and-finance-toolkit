using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace System.Windows.Input
{
    public class ReactiveCommand
        : ICommand
    {
        static ReactiveCommand()
        {
            _canAlwaysExecutePredicate  = static _ => true;
            _canAlwaysExecute           = Observable
                .Never<Predicate<object?>>()
                .Prepend(_canAlwaysExecutePredicate);
            _canNeverExecutePredicate   = static _ => false;
            _doNothingAction            = static _ => { };
            _doNothingOnExecuted        = Observer.Create(_doNothingAction);

            NotSupported = new(
                canExecute: Observable.Return(_canNeverExecutePredicate),
                executeOperation: static onExecuteRequested => onExecuteRequested.SelectUnit());
        }

        public static readonly ReactiveCommand NotSupported;

        public static ReactiveCommand Create(Action onExecuted)
            => new(
                canExecute: _canAlwaysExecute,
                onExecuted: Observer.Create<object?>(_ => onExecuted.Invoke()));

        public static ReactiveCommand Create(
                Action              onExecuted,
                IObservable<bool>   canExecute)
            => new(
                canExecute:         canExecute
                    .Select(static canExecute => canExecute
                        ? _canAlwaysExecutePredicate
                        : _canNeverExecutePredicate),
                onExecuted: Observer.Create<object?>(_ => onExecuted.Invoke()));

        public static ReactiveCommand Create(IObserver<Unit> onExecuted)
            => new(
                canExecute: _canAlwaysExecute,
                onExecuted: Observer.Create<object?>(_ => onExecuted.OnNext(Unit.Default)));

        public static ReactiveCommand Create(
                IObserver<Unit>     onExecuted,
                IObservable<bool>   canExecute)
            => new(
                canExecute: canExecute
                    .Select(static canExecute => canExecute
                        ? _canAlwaysExecutePredicate
                        : _canNeverExecutePredicate),
                onExecuted: Observer.Create<object?>(_ => onExecuted.OnNext(Unit.Default)));

        public static ReactiveCommand Create(ReactiveOperation<Unit, Unit> executeOperation)
            => new(
                canExecute:         _canAlwaysExecute,
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .SelectUnit()
                    .ApplyOperation(executeOperation));

        public static ReactiveCommand Create(
                ReactiveOperation<Unit, Unit>   executeOperation,
                IObservable<bool>               canExecute)
            => new(
                canExecute:         canExecute
                    .Select(static canExecute => canExecute
                        ? _canAlwaysExecutePredicate
                        : _canNeverExecutePredicate),
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .SelectUnit()
                    .ApplyOperation(executeOperation));

        public static ReactiveCommand Create<T>(ReactiveOperation<T, Unit>  executeOperation)
                where T : struct
            => new(
                canExecute:         _canAlwaysExecute,
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .Where(static parameter => parameter is not null and T)
                    .Select(static parameter => (T)parameter!)
                    .ApplyOperation(executeOperation));

        public static ReactiveCommand Create<T>(
                    ReactiveOperation<T, Unit>  executeOperation,
                    IObservable<Predicate<T?>>  canExecute)
                where T : struct
            => new(
                canExecute:         canExecute
                    .Select(static canExecute => new Predicate<object?>(
                        parameter => parameter switch
                        {
                            null    => canExecute.Invoke(null),
                            T value => canExecute.Invoke(value),
                            _       => false
                        })),
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .Where(static parameter => parameter is not null and T)
                    .Select(static parameter => (T)parameter!)
                    .ApplyOperation(executeOperation));

        private ReactiveCommand(
            IObservable<Predicate<object?>> canExecute,
            IObserver<object?>              onExecuted)
        {
            _canExecutePredicate    = _canNeverExecutePredicate;
            _onExecuted             = _doNothingOnExecuted;

            _canExecuteChanged = canExecute
                .Do(canExecute => 
                {
                    _canExecutePredicate    = canExecute;
                    _onExecuted             = onExecuted;
                })
                .Finally(() => 
                {
                    _canExecutePredicate    = _canNeverExecutePredicate;
                    _onExecuted             = _doNothingOnExecuted;
                })
                .SelectUnit()
                .Share()
                .ToEventPattern(sender: this);
        }

        private ReactiveCommand(
            IObservable<Predicate<object?>>     canExecute,
            ReactiveOperation<object?, Unit>    executeOperation)
        {
            _canExecutePredicate    = _canNeverExecutePredicate;
            _onExecuted             = _doNothingOnExecuted;

            _canExecuteChanged = Observable.Create<Unit>(observer =>
                {
                    var onExecuted = new Subject<object?>();
                    _onExecuted = onExecuted;

                    return new CompositeDisposable()
                    {
                        Disposable.Create(() =>
                        {
                            onExecuted.OnCompleted();

                            _canExecutePredicate    = _canNeverExecutePredicate;
                            _onExecuted             = _doNothingOnExecuted;

                            onExecuted.Dispose();
                        }),

                        onExecuted
                            .ApplyOperation(executeOperation)
                            .Subscribe(),

                        canExecute
                            .Do(canExecute => _canExecutePredicate = canExecute)
                            .SelectUnit()
                            .Subscribe(observer)
                    };
                })
                .Share()
                .ToEventPattern(sender: this);
        }

        event EventHandler? ICommand.CanExecuteChanged
        {
            add     => _canExecuteChanged.OnNext += value;
            remove  => _canExecuteChanged.OnNext -= value;
        }

        bool ICommand.CanExecute(object? parameter)
            => _canExecutePredicate.Invoke(parameter);

        void ICommand.Execute(object? parameter)
            => _onExecuted.OnNext(parameter);

        private readonly IBasicEventSource _canExecuteChanged;

        private Predicate<object?>  _canExecutePredicate;
        private IObserver<object?>  _onExecuted;

        private static readonly IObservable<Predicate<object?>> _canAlwaysExecute;
        private static readonly Predicate<object?>              _canAlwaysExecutePredicate;
        private static readonly Predicate<object?>              _canNeverExecutePredicate;
        private static readonly Action<object?>                 _doNothingAction;
        private static readonly IObserver<object?>              _doNothingOnExecuted;
    }
}
