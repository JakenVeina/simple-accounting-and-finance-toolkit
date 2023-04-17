using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

using Microsoft.Xaml.Behaviors;

namespace Saaft.Desktop.Behaviors
{
    public class LaunchWorkspaceBehavior
        : Behavior<UIElement>
    {
        public LaunchWorkspaceBehavior()
        {
            _commandBinding = new()
            {
                Command = Workspaces.Commands.Launch
            };
            _commandBinding.Executed += (sender, e) =>
            {
                var workspace = ((Func<Workspaces.ModelBase>)e.Parameter).Invoke();
                try
                {
                    new Workspaces.Window()
                        {
                            DataContext     = workspace,
                            SizeToContent   = SizeToContent.WidthAndHeight
                        }
                        .ShowDialog();
                }
                finally
                {
                    if (workspace is IDisposable disposable)
                        disposable.Dispose();
                }
            };
            _commandBinding.CanExecute += (sender, e) => e.CanExecute = true;
        }

        protected override void OnAttached()
            => AssociatedObject.CommandBindings.Add(_commandBinding);

        protected override void OnDetaching()
            => AssociatedObject.CommandBindings.Remove(_commandBinding);

        private readonly CommandBinding _commandBinding;
        
    }
}
