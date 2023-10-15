namespace System.Windows.Input
{
    public interface IActionCommand
        : ICommand
    {
        new bool CanExecute { get; }

        void Execute();
    }
}
