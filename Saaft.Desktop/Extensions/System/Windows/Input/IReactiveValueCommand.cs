namespace System.Windows.Input
{
    public interface IReactiveValueCommand<T>
            : IValueCommand<T>,
                IReactiveCommand
        where T : struct
    { }
}
