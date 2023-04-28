using System.Collections;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.ComponentModel
{
    public static class ReactiveProperty
    {
        public static ReactiveProperty<T> Create<T>(
                T           initialValue,
                ISubject<T> valueSource)
            => new(
                errorsSource:   _alwaysValid,
                initialValue:   initialValue,
                onValueSet:     valueSource,
                valueSource:    valueSource);

        public static ReactiveProperty<T> Create<T>(
                T                                   initialValue,
                ISubject<T>                         valueSource,
                IObservable<IReadOnlyList<object?>> errorSource)
            => new(
                errorsSource:   errorSource,
                initialValue:   initialValue,
                onValueSet:     valueSource,
                valueSource:    valueSource);

        internal static readonly DataErrorsChangedEventArgs ErrorsChangedEventArgs
            = new(nameof(ReactiveProperty<object>.Value));

        internal static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(ReactiveProperty<object>.Value));

        private static readonly IObservable<IReadOnlyList<object?>> _alwaysValid
            = Observable.Return(Array.Empty<object?>());
    }

    public class ReactiveProperty<T>
        : ReactiveReadOnlyProperty<T>,
            INotifyDataErrorInfo
    {
        internal ReactiveProperty(
                IObservable<IReadOnlyList<object?>> errorsSource,
                T                                   initialValue,
                IObserver<T>                        onValueSet,
                IObservable<T>                      valueSource)
            : base(
                initialValue:   initialValue,
                valueSource:    valueSource)
        {
            _errors     = Array.Empty<object?>();
            _onValueSet = onValueSet;

            var errorsChangedEventPattern = new EventPattern<DataErrorsChangedEventArgs>(this, ReactiveProperty.ErrorsChangedEventArgs);
            _errorsChanged = errorsSource
                .Do(errors => _errors = errors)
                .Finally(() => _errors = Array.Empty<object?>())
                .Select(_ => errorsChangedEventPattern)
                .Share()
                .ToEventPattern();
        }

        new public T Value
        {
            get => base.Value;
            set => _onValueSet.OnNext(value);
        }

        bool INotifyDataErrorInfo.HasErrors
            => _errors.Count is not 0;

        event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
        {
            add     => _errorsChanged.OnNext += value;
            remove  => _errorsChanged.OnNext -= value;
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
            => _errors;

        private readonly IEventPatternSource<DataErrorsChangedEventArgs>    _errorsChanged;
        private readonly IObserver<T>                                       _onValueSet;

        private IReadOnlyList<object?> _errors;
    }
}
