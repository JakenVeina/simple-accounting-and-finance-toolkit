using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;

namespace Saaft.Desktop.Extensions.System.ComponentModel
{
    internal static class ReactiveValueBase
    {
        internal static readonly IObservable<IReadOnlyList<object?>> AlwaysValidErrorsSource
            = Observable.Return(Array.Empty<object?>());

        internal static readonly DataErrorsChangedEventArgs ErrorsChangedEventArgs
            = new(nameof(ReactiveValue<object>.Value));

        internal static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(ReactiveValue<object>.Value));
    }

    public abstract class ReactiveValueBase<T>
        : INotifyPropertyChanged,
            INotifyDataErrorInfo
    {
        protected static IPropertyChangedEventSource BuildPropertyChangedSource(
            ReactiveValueBase<T>    sender,
            T                       initialValue,
            IObservable<T>          valueSource)
        {
            var propertyChangedEventPattern = new EventPattern<object?, PropertyChangedEventArgs>(sender, ReactiveValueBase.ValueChangedEventArgs);
            return valueSource
                .Do(value => sender._value = value)
                .Finally(() => sender._value = initialValue)
                .Select(_ => propertyChangedEventPattern)
                .Share()
                .ToEventPattern();
        }

        protected ReactiveValueBase(
            IObservable<IReadOnlyList<object?>> errorsSource,
            T                                   initialValue)
        {
            var errorsChangedEventPattern = new EventPattern<DataErrorsChangedEventArgs>(this, ReactiveValueBase.ErrorsChangedEventArgs);
            _errorsChanged = errorsSource
                .Do(errors => _errors = errors)
                .Finally(() => _errors = Array.Empty<object?>())
                .Select(_ => errorsChangedEventPattern)
                .Share()
                .ToEventPattern();

            _errors = Array.Empty<object?>();
            _value  = initialValue;
        }

        protected T Value
            => _value;

        protected abstract event PropertyChangedEventHandler? PropertyChanged;

        bool INotifyDataErrorInfo.HasErrors
            => _errors.Count is not 0;

        event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
        {
            add     => _errorsChanged.OnNext += value;
            remove  => _errorsChanged.OnNext -= value;
        }

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add     => PropertyChanged += value;
            remove  => PropertyChanged -= value;
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
            => _errors;

        private readonly IEventPatternSource<DataErrorsChangedEventArgs> _errorsChanged;

        private IReadOnlyList<object?>  _errors;
        private T                       _value;
    }
}
