using System.Windows.Input;

namespace Saaft.Desktop.Workspaces
{
    public static class Commands
    {
        public static RoutedCommand Launch { get; }
            = new(
                name:       nameof(Launch),
                ownerType:  typeof(Commands));
    }
}
