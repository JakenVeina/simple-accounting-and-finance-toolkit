using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace System.Windows.Input
{
    public static class ReactiveCommand
    {
        public static ReactiveCommand<Unit> Create(Action onExecuted)
            => new(
                onExecuted:         Observer.Create<Unit>(_ => onExecuted.Invoke()),
                canExecute:         _canExecuteTrue,
                parameterConverter: _convertUnit);

        public static ReactiveCommand<Unit> Create(IObserver<Unit> onExecuted)
            => new(
                onExecuted:         onExecuted,
                canExecute:         _canExecuteTrue,
                parameterConverter: _convertUnit);
    
        public static ReactiveCommand<Unit> Create(
                IObserver<Unit>     onExecuted,
                IObservable<bool>   canExecute)
            => new(
                onExecuted:         onExecuted,
                canExecute:         canExecute,
                parameterConverter: _convertUnit);

        private static readonly IObservable<bool> _canExecuteTrue
            = Observable.Return(true);

        private static readonly Func<object?, Unit> _convertUnit
            = _ => default;
    }

    public sealed class ReactiveCommand<T>
        : ICommand
    {
        internal ReactiveCommand(
            IObserver<T>        onExecuted,
            IObservable<bool>   canExecute,
            Func<object?, T>    parameterConverter)
        {
            _canExecute             = false;
            _onExecuted             = onExecuted;
            _parameterConverter     = parameterConverter;
            _subscriptionsByHandler = new();

            _canExecuteSource       = canExecute
                .Do(canExecute => _canExecute = canExecute)
                .Share();
        }

        event EventHandler? ICommand.CanExecuteChanged
        {
            add
            {
                if ((value is not null) && !_subscriptionsByHandler.ContainsKey(value))
                    _subscriptionsByHandler.Add(value, _canExecuteSource.Subscribe(sourceValue => value.Invoke(this, EventArgs.Empty)));
            }
            remove
            {
                if ((value is not null) && _subscriptionsByHandler.TryGetValue(value, out var subscription))
                {
                    _subscriptionsByHandler.Remove(value);
                    subscription.Dispose();
                }
            }
        }

        bool ICommand.CanExecute(object? parameter)
            => _canExecute;

        void ICommand.Execute(object? parameter)
            => _onExecuted.OnNext(_parameterConverter.Invoke(parameter));

        private readonly IObservable<bool>                      _canExecuteSource;
        private readonly IObserver<T>                           _onExecuted;
        private readonly Func<object?, T>                       _parameterConverter;
        private readonly Dictionary<EventHandler, IDisposable>  _subscriptionsByHandler;

        private bool _canExecute;
    }
}
