using System.Reactive.Linq;

namespace System.Reactive
{
    public interface IEventPatternSource
    {
        event EventHandler? OnNext;
    }

    public class EventPatternSource
        : EventPatternSourceBase<object?, EventArgs>,
            IEventPatternSource
    {
        public static EventPatternSource Create(
            object?             sender,
            IObservable<Unit>   source)
        {
            var pattern = new EventPattern<object?, EventArgs>(sender, EventArgs.Empty);

            return new EventPatternSource(source.Select(_ => pattern));
        }

        private EventPatternSource(IObservable<EventPattern<object?, EventArgs>> source)
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
