using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;

namespace System.Windows.Input
{
    public abstract class ReactiveCommandBase
        : DisposableBase,
            IReactiveCommand
    {
        protected ReactiveCommandBase(IObservable<Unit> canExecuteChanged)
        {
            _canExecuteChanged      = canExecuteChanged.Publish();
            _canExecuteChangedEvent = BasicEventSource.Create(this, _canExecuteChanged);
            _canExecuteConnection   = _canExecuteChanged.Connect();
        }

        public IObservable<Unit> CanExecuteChanged
            => _canExecuteChanged;

        protected abstract bool OnCanExecute(object? parameter);

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
                _canExecuteConnection.Dispose();
        }

        protected abstract void OnExecute(object? parameter);

        protected void ThrowUnsupportedParameterType(
                Type    parameterType,
                Type    expectedType,
                string  paramName)
            => throw new ArgumentException($"Unsupported parameter type {parameterType}: expected {expectedType}", paramName);

        event EventHandler? ICommand.CanExecuteChanged
        {
            add     => _canExecuteChangedEvent.OnNext += value;
            remove  => _canExecuteChangedEvent.OnNext -= value;
        }

        bool ICommand.CanExecute(object? parameter)
            => OnCanExecute(parameter);

        void ICommand.Execute(object? parameter)
            => OnExecute(parameter);

        private readonly IConnectableObservable<Unit>   _canExecuteChanged;
        private readonly BasicEventSource               _canExecuteChangedEvent;
        private readonly IDisposable                    _canExecuteConnection;
    }
}
