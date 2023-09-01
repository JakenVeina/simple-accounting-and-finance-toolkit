using System.Reactive;
using System.Reactive.Linq;

namespace System.ComponentModel
{
    public static class ReactiveReadOnlyValue
    {
        public static ReactiveReadOnlyValue<T> Create<T>(T value)
            => new(
                initialValue:   value,
                valueSource:    Observable.Empty<T>());

        public static ReactiveReadOnlyValue<T?> Create<T>(IObservable<T?> source)
            => new(
                initialValue:   default,
                valueSource:    source);

        public static ReactiveReadOnlyValue<T> Create<T>(
                IObservable<T>  source,
                T               initialValue)
            => new(
                initialValue:   initialValue,
                valueSource:    source);

        internal static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(ReactiveReadOnlyValue<object>.Value));
    }

    public class ReactiveReadOnlyValue<T>
        : INotifyPropertyChanged
    {
        internal ReactiveReadOnlyValue(
            T                                   initialValue,
            IObservable<T>                      valueSource)
        {
            _value = initialValue;

            var propertyChangedEventPattern = new EventPattern<object?, PropertyChangedEventArgs>(this, ReactiveReadOnlyValue.ValueChangedEventArgs);
            _propertyChanged = valueSource
                .Do(value => _value = value)
                .Finally(() => _value = initialValue)
                .Select(_ => propertyChangedEventPattern)
                .Share()
                .ToEventPattern();
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
