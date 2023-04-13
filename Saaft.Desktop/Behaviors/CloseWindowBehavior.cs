using System;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Behaviors
{
    public class CloseWindowBehavior
        : Behavior<Window>
    {
        public CloseWindowBehavior()
        {
            _commandBinding = new CommandBinding()
            {
                Command = ApplicationCommands.Close
            };
            _commandBinding.Executed    += (sender, e) => AssociatedObject.Close();
        }

        protected override void OnAttached()
            => AssociatedObject.CommandBindings.Add(_commandBinding);

        protected override void OnDetaching()
            => AssociatedObject.CommandBindings.Remove(_commandBinding);

        private readonly CommandBinding _commandBinding;
    }
}
