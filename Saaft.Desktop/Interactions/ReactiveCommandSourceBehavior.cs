using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Interactions
{
    public class ReactiveCommandSourceBehavior
        : Behavior<DependencyObject>
    {
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty
            = DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(ReactiveCommandSourceBehavior),
                new PropertyMetadata()
                {
                    PropertyChangedCallback = (sender, e) => ((ReactiveCommandSourceBehavior)sender).OnCommandChanged(e)
                });

        public IObservable<object?>? Source
        {
            get => (IObservable<object?>?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty
            = DependencyProperty.Register(
                nameof(Source),
                typeof(IObservable<object?>),
                typeof(ReactiveCommandSourceBehavior),
                new PropertyMetadata()
                {
                    PropertyChangedCallback = (sender, e) => ((ReactiveCommandSourceBehavior)sender).OnSourceChanged(e)
                });

        private void OnCommandChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ICommand oldvalue)
            {
                oldvalue.CanExecuteChanged -= OnCommandCanExecuteChanged;
                _commandCanExecute = false;
                if (_subscription is not null)
                {
                    _subscription.Dispose();
                    _subscription = null;
                }
            }

            if (e.NewValue is ICommand newValue)
            {
                _commandCanExecute = newValue.CanExecute(null);
                if (!_hasSourceCompleted && (Source is IObservable<object?> source))
                {
                    newValue.CanExecuteChanged += OnCommandCanExecuteChanged;
                    if (_commandCanExecute)
                        _subscription = source.Subscribe(OnSourceNext, OnSourceCompleted);
                }
            }
        }

        private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            _commandCanExecute = Command!.CanExecute(null);
            if (_commandCanExecute
                    && !_hasSourceCompleted
                    && (_subscription is null)
                    && (Source is IObservable<object?> source))
                _subscription = source.Subscribe(OnSourceNext, OnSourceCompleted);
            else if (!_commandCanExecute && (_subscription is not null))
            {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        private void OnSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            if (_subscription is not null)
            {
                _subscription.Dispose();
                _subscription = null;
            }

            _hasSourceCompleted = false;

            if (e.NewValue is IObservable<object?> newValue)
            {
                if (Command is ICommand command)
                    command.CanExecuteChanged += OnCommandCanExecuteChanged;
                if (_commandCanExecute)
                    _subscription = newValue.Subscribe(OnSourceNext, OnSourceCompleted);
            }
            else
            {
                if (Command is ICommand command)
                    command.CanExecuteChanged -= OnCommandCanExecuteChanged;
            }
        }

        private void OnSourceCompleted()
        {
            _hasSourceCompleted = true;
            if (Command is ICommand command)
                command.CanExecuteChanged -= OnCommandCanExecuteChanged;
            if (_subscription is not null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        private void OnSourceNext(object? parameter)
            => Command?.Execute(parameter);

        private bool            _commandCanExecute;
        private bool            _hasSourceCompleted;
        private IDisposable?    _subscription;
    }
}
