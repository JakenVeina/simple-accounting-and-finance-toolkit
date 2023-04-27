using System.ComponentModel;

namespace System.Reactive
{
    public interface IPropertyChangedEventSource
    {
        event PropertyChangedEventHandler? OnNext;
    }

    public class PropertyChangedEventSource
        : EventPatternSourceBase<object?, PropertyChangedEventArgs>,
            IPropertyChangedEventSource
    {
        public PropertyChangedEventSource(IObservable<EventPattern<object?, PropertyChangedEventArgs>> source)
            : base(
                source:         source,
                invokeHandler:  static (onExecuted, pattern) => onExecuted.Invoke(pattern.Sender, pattern.EventArgs))
        { }

        public event PropertyChangedEventHandler? OnNext
        {
            add
            {
                if (value is not null)
                    Add(value, value.Invoke);
            }
            remove
            {
                if (value is not null)
                    Remove(value);
            }
        }
    }
}
