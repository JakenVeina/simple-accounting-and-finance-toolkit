using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

using Saaft.Desktop.Extensions.System.ComponentModel;

namespace System.ComponentModel
{
    public static class ReactiveReadOnlyValue
    {
        public static ReactiveReadOnlyValue<T> Create<T>(T value)
            => new(
                errorsSource:   ReactiveValueBase.AlwaysValidErrorsSource,
                initialValue:   value,
                valueSource:    Observable.Never<T>());

        public static ReactiveReadOnlyValue<T?> Create<T>(IObservable<T?> source)
            => new(
                errorsSource:   ReactiveValueBase.AlwaysValidErrorsSource,
                initialValue:   default,
                valueSource:    source);

        public static ReactiveReadOnlyValue<T> Create<T>(
                IObservable<T>  source,
                T               initialValue)
            => new(
                errorsSource:   ReactiveValueBase.AlwaysValidErrorsSource,
                initialValue:   initialValue,
                valueSource:    source);
    }

    public class ReactiveReadOnlyValue<T>
        : ReactiveValueBase<T>
    {
        internal ReactiveReadOnlyValue(
                    IObservable<IReadOnlyList<object?>> errorsSource,
                    T                                   initialValue,
                    IObservable<T>                      valueSource)
                : base(
                    errorsSource:   errorsSource,
                    initialValue:   initialValue)
            => _propertyChanged = BuildPropertyChangedSource(this, initialValue, valueSource);

        new public T Value
            => Value;

        protected override event PropertyChangedEventHandler? PropertyChanged
        {
            add     => _propertyChanged.OnNext += value;
            remove  => _propertyChanged.OnNext -= value;
        }

        private readonly IPropertyChangedEventSource _propertyChanged;
    }
}
