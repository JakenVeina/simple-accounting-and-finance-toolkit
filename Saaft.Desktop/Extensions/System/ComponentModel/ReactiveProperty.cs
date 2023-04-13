using System.Collections.Generic;
using System.Reactive.Linq;

namespace System.ComponentModel
{
    public abstract class ReactiveProperty
        : INotifyPropertyChanged
    {
        public static ReactiveProperty<T> Create<T>(T value)
            => new(
                initialValue:   value,
                source:         Observable.Empty<T>());

        public static ReactiveProperty<T?> Create<T>(IObservable<T?> source)
            => new(
                initialValue:   default,
                source:         source);

        public static ReactiveProperty<T> Create<T>(
                IObservable<T>  source,
                T               initialValue)
            => new(
                initialValue:   initialValue,
                source:         source);

        public ReactiveProperty()
            => _subscriptionsByHandler = new();

        protected abstract IDisposable OnSubscribing(PropertyChangedEventHandler handler);

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                if ((value is not null) && !_subscriptionsByHandler.ContainsKey(value))
                    _subscriptionsByHandler.Add(value, OnSubscribing(value));
            }
            remove
            {
                if ((value is not null) && _subscriptionsByHandler.TryGetValue(value, out var subscription))
                {
                    _subscriptionsByHandler.Remove(value);
                    subscription.Dispose();
                }
            }
        }

        private readonly Dictionary<PropertyChangedEventHandler, IDisposable> _subscriptionsByHandler;

        protected static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(ReactiveProperty<object>.Value));
    }

    public sealed class ReactiveProperty<T>
        : ReactiveProperty,
            IObservable<T>
    {
        internal ReactiveProperty(
            T               initialValue,
            IObservable<T>  source)
        {
            _value = initialValue;

            _source = source
                .Do(value => _value = value)
                .ShareReplay(1);
        }

        public T Value
            => _value;

        public IDisposable Subscribe(IObserver<T> observer)
            => _source.Subscribe(observer);

        protected override IDisposable OnSubscribing(PropertyChangedEventHandler handler)
            => _source.Skip(1).Subscribe(_ => handler.Invoke(this, ValueChangedEventArgs));

        private readonly IObservable<T> _source;

        private T _value;
    }
}
