using System.Reactive.Linq;

namespace System.Reactive
{
    public interface IBasicEventSource
    {
        event EventHandler? OnNext;
    }

    public class BasicEventSource
        : EventPatternSourceBase<object?, EventArgs>,
            IBasicEventSource
    {
        public static BasicEventSource Create(
            object?             sender,
            IObservable<Unit>   source)
        {
            var pattern = new EventPattern<object?, EventArgs>(sender, EventArgs.Empty);

            return new BasicEventSource(source.Select(_ => pattern));
        }

        private BasicEventSource(IObservable<EventPattern<object?, EventArgs>> source)
            : base(
                source:         source,
                invokeHandler:  static (onExecuted, pattern) => onExecuted.Invoke(pattern.Sender, pattern.EventArgs))
        { }

        public event EventHandler? OnNext
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
