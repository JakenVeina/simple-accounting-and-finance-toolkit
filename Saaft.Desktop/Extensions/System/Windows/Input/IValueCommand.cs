namespace System.Windows.Input
{
    public interface IValueCommand<T>
            : ICommand
        where T : struct
    {
        void Execute(T parameter);

        bool CanExecute(T? parameter);
    }
}
