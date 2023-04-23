using System.Collections;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.ComponentModel
{
    internal static class ObservableProperty
    {
        internal static readonly DataErrorsChangedEventArgs ErrorsChangedEventArgs
            = new(nameof(ObservableProperty<object>.Value));

        internal static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(ObservableProperty<object>.Value));
    }

    public class ObservableProperty<T>
        : IObservable<T>,
            INotifyPropertyChanged,
            INotifyDataErrorInfo,
            IDisposable
    {
        public ObservableProperty(T initialValue)
            : this(
                  initialValue: initialValue,
                  validator:    value => value.Select(_ => Array.Empty<object?>()))
        { }

        public ObservableProperty(
            T                                                           initialValue,
            Func<IObservable<T>, IObservable<IReadOnlyList<object?>>>   validator)
        {
            _errors = Array.Empty<object?>();
            _value  = new(initialValue);

            var value = _value
                .DistinctUntilChanged()
                .ShareReplay(1);

            _errorsSource = validator.Invoke(value)
                .Do(errors => _errors = errors)
                .ShareReplay(1);

            var errorsChangedEventPattern = new EventPattern<DataErrorsChangedEventArgs>(this, ObservableProperty.ErrorsChangedEventArgs);
            _errorsChanged = _errorsSource
                .Select(_ => errorsChangedEventPattern)
                .ToEventPattern();

            _hasErrors = _errorsSource
                .Select(errors => errors.Count is not 0)
                .DistinctUntilChanged();

            var propertyChangedEventPattern = new EventPattern<object?, PropertyChangedEventArgs>(this, ObservableProperty.ValueChangedEventArgs);
            _propertyChanged = value
                .Select(_ => propertyChangedEventPattern)
                .ToEventPattern();
        }

        public IObservable<bool> HasErrors
            => _hasErrors;

        public T Value
        {
            get => _value.Value;
            set => _value.OnNext(value);
        }

        public void Dispose()
        {
            _value.OnCompleted();
            _value.Dispose();
        }

        public IDisposable Subscribe(IObserver<T> observer)
            => _value.Subscribe(observer);

        bool INotifyDataErrorInfo.HasErrors
            => _errors.Count is not 0;

        event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
        {
            add     => _errorsChanged.OnNext += value;
            remove  => _errorsChanged.OnNext -= value;
        }

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add     => _propertyChanged.OnNext += value;
            remove  => _propertyChanged.OnNext -= value;
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
            => _errors;

        private readonly IEventPatternSource<DataErrorsChangedEventArgs>    _errorsChanged;
        private readonly IObservable<IReadOnlyList<object?>>                _errorsSource;
        private readonly IObservable<bool>                                  _hasErrors;
        private readonly IPropertyChangedEventSource                        _propertyChanged;
        private readonly BehaviorSubject<T>                                 _value;

        private IReadOnlyList<object?> _errors;
    }
}
