using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Interactions
{
    public class ReceiveDropCommandBehavior
        : Behavior<UIElement>
    {
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty
            = DependencyProperty.Register(
                name:           nameof(Command),
                propertyType:   typeof(ICommand),
                ownerType:      typeof(ReceiveDropCommandBehavior),
                typeMetadata:   new PropertyMetadata(
                    defaultValue:               null,
                    propertyChangedCallback:    (sender, e) => ((ReceiveDropCommandBehavior)sender).OnCommandChanged(e)));

        public string? DataFormat
        {
            get => _dataFormat;
            set => _dataFormat = value;
        }

        protected override void OnAttached()
        {
            AssociatedObject.AllowDrop = Command?.CanExecute(null) ?? false;
            AssociatedObject.DragOver   += OnAssociatedObjectDragOver;
            AssociatedObject.Drop       += OnAssociatedObjectDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.AllowDrop = Command?.CanExecute(null) ?? false;
            AssociatedObject.DragOver   -= OnAssociatedObjectDragOver;
            AssociatedObject.Drop       -= OnAssociatedObjectDrop;
        }

        private void HandleDragEvent(
            DragEventArgs   e,
            bool            executeCommand)
        {
            if ((Command is not ICommand command)
                || (_dataFormat is not string dataFormat))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var dataValue = e.Data.GetData(dataFormat);
            if (dataValue is null)
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (!command.CanExecute(dataValue))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            if (executeCommand)
                command.Execute(dataValue);

            e.Handled = true;
        }

        private void OnAssociatedObjectDragOver(object? sender, DragEventArgs e)
            => HandleDragEvent(e, executeCommand: false);

        private void OnAssociatedObjectDrop(object? sender, DragEventArgs e)
            => HandleDragEvent(e, executeCommand: true);

        private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            if (AssociatedObject is UIElement element)
                element.AllowDrop = Command?.CanExecute(null) ?? false;
        }

        private void OnCommandChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ICommand oldCommand)
                oldCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;

            if (e.NewValue is ICommand newCommand)
            {
                newCommand.CanExecuteChanged += OnCommandCanExecuteChanged;

                if (AssociatedObject is UIElement element)
                    element.AllowDrop = newCommand.CanExecute(null);
            }
            else if (AssociatedObject is UIElement element)
                element.AllowDrop = false;
        }

        private string? _dataFormat;
    }
}
