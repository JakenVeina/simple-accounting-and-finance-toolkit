using System.Reactive;
using System.Reactive.Linq;

namespace System.ComponentModel
{
    public static class ReactiveProperty
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

        internal static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(ReactiveProperty<object>.Value));
    }

    public class ReactiveProperty<T>
        : INotifyPropertyChanged
    {
        internal ReactiveProperty(
            T               initialValue,
            IObservable<T>  source)
        {
            var pattern = new EventPattern<object?, PropertyChangedEventArgs>(
                sender: this,
                e:      ReactiveProperty.ValueChangedEventArgs);

            _propertyChanged = source
                .Do(value => _value = value)
                .Finally(() => _value = initialValue)
                .Select(_ => pattern)
                .ShareReplay(1)
                .ToEventPattern();

            _value = initialValue;
        }

        public T Value
            => _value;

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add     => _propertyChanged.OnNext += value;
            remove  => _propertyChanged.OnNext -= value;
        }

        private readonly IPropertyChangedEventSource _propertyChanged;

        private T _value;
    }
}
