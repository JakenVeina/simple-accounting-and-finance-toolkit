using System.Reactive;
using System.Reactive.Linq;

namespace System.Windows.Input
{
    public class ReactiveCommand
        : ICommand
    {
        static ReactiveCommand()
        {
            _canAlwaysExecutePredicate  = _ => true;
            _canAlwaysExecute           = Observable.Return(_canAlwaysExecutePredicate);
            _canNeverExecutePredicate   = _ => false;

            NotSupported = new(
                canExecute: Observable.Return(_canNeverExecutePredicate),
                onExecuted: Observer.Create<object?>(_ => { }));
        }

        public static readonly ReactiveCommand NotSupported;

        public static ReactiveCommand Create(Action onExecuted)
            => new(
                canExecute: _canAlwaysExecute,
                onExecuted: Observer.Create<object?>(_ => onExecuted.Invoke()));

        public static ReactiveCommand Create(IObserver<Unit> onExecuted)
            => new(
                canExecute: _canAlwaysExecute,
                onExecuted: Observer.Create<object?>(_ => onExecuted.OnNext(default)));
    
        public static ReactiveCommand Create(
                IObserver<Unit>     onExecuted,
                IObservable<bool>   canExecute)
            => new(
                canExecute: canExecute
                    .Select(canExecute => canExecute
                        ? _canAlwaysExecutePredicate
                        : _canNeverExecutePredicate),
                onExecuted: Observer.Create<object?>(_ => onExecuted.OnNext(default)));

        public static ReactiveCommand Create<T>(IObserver<T> onExecuted)
            => new(
                canExecute: Observable.Return(new Predicate<object?>(parameter => parameter switch
                {
                    null    => true,
                    T value => true,
                    _       => false
                })),
                onExecuted: Observer.Create<object?>(parameter => 
                {
                    if (parameter is T value)
                        onExecuted.OnNext(value);
                }));

        public static ReactiveCommand Create<T>(
                    IObserver<T>                onExecuted,
                    IObservable<Predicate<T?>>  canExecute)
                where T : struct
            => new(
                canExecute: canExecute
                    .Select(canExecutePredicate => new Predicate<object?>(parameter => parameter switch
                    {
                        null    => canExecutePredicate.Invoke(default),
                        T value => canExecutePredicate.Invoke(value),
                        _       => false
                    })),
                onExecuted: Observer.Create<object?>(parameter => 
                {
                    if (parameter is T value)
                        onExecuted.OnNext(value);
                }));

        private ReactiveCommand(
            IObservable<Predicate<object?>> canExecute,
            IObserver<object?>              onExecuted)
        {
            _canExecute = _canNeverExecutePredicate;
            _onExecuted = onExecuted;

            var eventPattern = new EventPattern<object?, EventArgs>(this, EventArgs.Empty);

            _canExecuteChanged = canExecute
                .Do(canExecute => _canExecute = canExecute)
                .Select(_ => Unit.Default)
                .Share()
                .ToEventPattern(sender: this);
        }

        event EventHandler? ICommand.CanExecuteChanged
        {
            add     => _canExecuteChanged.OnNext += value;
            remove  => _canExecuteChanged.OnNext -= value;
        }

        bool ICommand.CanExecute(object? parameter)
            => _canExecute.Invoke(parameter);

        void ICommand.Execute(object? parameter)
            => _onExecuted.OnNext(parameter);

        private readonly IEventPatternSource    _canExecuteChanged;
        private readonly IObserver<object?>     _onExecuted;

        private Predicate<object?> _canExecute;

        private static readonly IObservable<Predicate<object?>> _canAlwaysExecute;
        private static readonly Predicate<object?>              _canAlwaysExecutePredicate;
        private static readonly Predicate<object?>              _canNeverExecutePredicate;
    }
}
