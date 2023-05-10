using System.Windows.Input;

namespace Saaft.Desktop
{
    public static class Commands
    {
        public static RoutedCommand Host { get; }
            = new(
                name:       nameof(Host),
                ownerType:  typeof(Commands));
    }
}
