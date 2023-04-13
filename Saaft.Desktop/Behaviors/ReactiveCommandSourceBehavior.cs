using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Behaviors
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
                    PropertyChangedCallback = (sender, e) =>
                    {
                        var @this = (ReactiveCommandSourceBehavior)sender;

                        if (e.OldValue is ICommand oldvalue)
                        {
                            oldvalue.CanExecuteChanged -= @this.OnCommandCanExecuteChanged;
                            @this._commandCanExecute = false;
                            @this.ManageSubscription();
                        }

                        if (e.NewValue is ICommand newValue)
                        {
                            @this._commandCanExecute = newValue.CanExecute(null);
                            newValue.CanExecuteChanged += @this.OnCommandCanExecuteChanged;
                            @this.ManageSubscription();
                        }
                    }
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
                    PropertyChangedCallback = (sender, e) => ((ReactiveCommandSourceBehavior)sender).ManageSubscription()
                });

        private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            _commandCanExecute = Command!.CanExecute(null);
            ManageSubscription();
        }

        private void ManageSubscription()
        {
            if (_commandCanExecute
                    && (Source is IObservable<object?> source)
                    && (Command is ICommand command))
                _subscription ??= source.Subscribe(command.Execute);
            else if (_subscription is not null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        private bool            _commandCanExecute;
        private IDisposable?    _subscription;
    }
}
