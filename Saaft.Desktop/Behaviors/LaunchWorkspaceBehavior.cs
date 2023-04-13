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
            _commandBinding = new();
            _commandBinding.Executed += (sender, e) =>
            {
                if (e.Parameter is null)
                    return;

                new Workspaces.Window()
                    {
                        DataContext     = e.Parameter,
                        SizeToContent   = SizeToContent.WidthAndHeight
                    }
                    .ShowDialog();
            };
            _commandBinding.CanExecute += (sender, e) => e.CanExecute = true;
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        public static readonly DependencyProperty CommandProperty
            = DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(LaunchWorkspaceBehavior),
                new PropertyMetadata()
                {
                    PropertyChangedCallback = (sender, e) => ((LaunchWorkspaceBehavior)sender)._commandBinding.Command = e.NewValue as ICommand
                });

        protected override void OnAttached()
            => AssociatedObject.CommandBindings.Add(_commandBinding);

        protected override void OnDetaching()
            => AssociatedObject.CommandBindings.Remove(_commandBinding);

        private readonly CommandBinding _commandBinding;
        
    }
}
