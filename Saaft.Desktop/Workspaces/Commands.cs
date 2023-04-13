using System.Windows.Input;

namespace Saaft.Desktop.Workspaces
{
    public static class Commands
    {
        public static RoutedCommand Interrupt { get; }
            = new(
                name:       nameof(Interrupt),
                ownerType:  typeof(Commands));
    }
}
