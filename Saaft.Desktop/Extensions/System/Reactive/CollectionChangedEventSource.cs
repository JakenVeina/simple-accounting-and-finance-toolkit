using System.Collections.Specialized;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace System.Reactive
{
    public interface ICollectionChangedEventSource
    {
        event NotifyCollectionChangedEventHandler? OnNext;
    }

    public class CollectionChangedEventSource
        : EventPatternSourceBase<object?, NotifyCollectionChangedEventArgs>,
            ICollectionChangedEventSource
    {
        public CollectionChangedEventSource(IObservable<EventPattern<object?, NotifyCollectionChangedEventArgs>> source)
            : base(
                source:         source,
                invokeHandler:  static (onExecuted, pattern) => onExecuted.Invoke(pattern.Sender, pattern.EventArgs))
        { }

        public event NotifyCollectionChangedEventHandler? OnNext
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
