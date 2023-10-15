using System.Reactive;

namespace System.Windows.Input
{
    public interface IReactiveCommand
        : ICommand
    {
        new IObservable<Unit> CanExecuteChanged { get; }
    }
}
