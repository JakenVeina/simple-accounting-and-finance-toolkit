using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.ComponentModel
{
    public sealed class ObservableProperty<T>
        : IObservable<T>,
            INotifyPropertyChanged,
            INotifyDataErrorInfo,
            IDisposable
    {
        public ObservableProperty(T initialValue)
            : this(
                  initialValue:     initialValue,
                  errorsFactory:    value => value.Select(_ => Array.Empty<object?>()))
        { }

        public ObservableProperty(
            T                                                           initialValue,
            Func<IObservable<T>, IObservable<IReadOnlyList<object?>>>   errorsFactory)
        {
            _errors                                 = Array.Empty<object?>();
            _errorsChangedSubscriptionsByHandler    = new();
            _propertyChangedSubscriptionsByHandler  = new();
            _valueSource                            = new(initialValue);

            _errorsSource = errorsFactory.Invoke(_valueSource)
                .Do(errors => _errors = errors)
                .ShareReplay(1);

            _hasErrors = _errorsSource
                .Select(errors => errors.Count is not 0);
        }

        public IObservable<bool> HasErrors
            => _hasErrors;

        public T Value
        {
            get => _valueSource.Value;
            set => _valueSource.OnNext(value);
        }

        public void Dispose()
            => _valueSource.Dispose();

        public IDisposable Subscribe(IObserver<T> observer)
            => _valueSource.Subscribe(observer);

        bool INotifyDataErrorInfo.HasErrors
            => _errors.Count is not 0;

        event EventHandler<DataErrorsChangedEventArgs>? INotifyDataErrorInfo.ErrorsChanged
        {
            add
            {
                if ((value is not null) && !_errorsChangedSubscriptionsByHandler.ContainsKey(value))
                    _errorsChangedSubscriptionsByHandler.Add(value, _errorsSource.Subscribe(_ => value.Invoke(this, ErrorsChangedEventArgs)));
            }
            remove
            {
                if ((value is not null) && _errorsChangedSubscriptionsByHandler.TryGetValue(value, out var subscription))
                {
                    _errorsChangedSubscriptionsByHandler.Remove(value);
                    subscription.Dispose();
                }
            }
        }

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                if ((value is not null) && !_propertyChangedSubscriptionsByHandler.ContainsKey(value))
                    _propertyChangedSubscriptionsByHandler.Add(value, _valueSource.Subscribe(_ => value.Invoke(this, ValueChangedEventArgs)));
            }
            remove
            {
                if ((value is not null) && _propertyChangedSubscriptionsByHandler.TryGetValue(value, out var subscription))
                {
                    _propertyChangedSubscriptionsByHandler.Remove(value);
                    subscription.Dispose();
                }
            }
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
            => _errors;

        private readonly Dictionary<EventHandler<DataErrorsChangedEventArgs>, IDisposable>  _errorsChangedSubscriptionsByHandler;
        private readonly IObservable<IReadOnlyList<object?>>                                _errorsSource;
        private readonly IObservable<bool>                                                  _hasErrors;
        private readonly Dictionary<PropertyChangedEventHandler, IDisposable>               _propertyChangedSubscriptionsByHandler;
        private readonly BehaviorSubject<T>                                                 _valueSource;

        private IReadOnlyList<object?> _errors;

        private static readonly DataErrorsChangedEventArgs ErrorsChangedEventArgs
            = new(nameof(Value));

        private static readonly PropertyChangedEventArgs ValueChangedEventArgs
            = new(nameof(Value));
    }
}
