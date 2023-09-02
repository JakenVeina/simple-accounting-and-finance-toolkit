using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Saaft.Desktop.Extensions.System.ComponentModel;

namespace System.ComponentModel
{
    public static class ReactiveValue
    {
        public static ReactiveValue<T> Create<T>(
                T           initialValue,
                ISubject<T> valueSource)
            => new(
                errorsSource:   ReactiveValueBase.AlwaysValidErrorsSource,
                initialValue:   initialValue,
                onValueSet:     valueSource,
                valueSource:    valueSource);

        public static ReactiveValue<T> Create<T>(
                T                                   initialValue,
                ISubject<T>                         valueSource,
                IObservable<IReadOnlyList<object?>> errorsSource)
            => new(
                errorsSource:   errorsSource,
                initialValue:   initialValue,
                onValueSet:     valueSource,
                valueSource:    valueSource);
    }

    public sealed class ReactiveValue<T>
        : ReactiveValueBase<T>
    {
        internal ReactiveValue(
                IObservable<IReadOnlyList<object?>> errorsSource,
                T                                   initialValue,
                IObserver<T>                        onValueSet,
                IObservable<T>                      valueSource)
            : base(
                errorsSource:   errorsSource,
                initialValue:   initialValue)
        {
            _propertyChanged = BuildPropertyChangedSource(this, initialValue, valueSource);

            _onValueSet = onValueSet;
        }

        internal ReactiveValue(
                IObservable<IReadOnlyList<object?>> errorsSource,
                T                                   initialValue,
                ReactiveOperation<T, Unit>          setValueOperation,
                IObservable<T>                      valueSource)
            : base(
                errorsSource:   errorsSource,
                initialValue:   initialValue)
        {
            _propertyChanged = BuildPropertyChangedSource(this, initialValue, Observable.Create<T>(observer =>
            {
                var onValueSet = new Subject<T>();
                _onValueSet = onValueSet;

                return new CompositeDisposable()
                {
                    Disposable.Create(() =>
                    {
                        onValueSet.OnCompleted();

                        _onValueSet = _doNothingOnValueSet;

                        onValueSet.Dispose();
                    }),

                    onValueSet
                        .ApplyOperation(setValueOperation)
                        .Subscribe(),

                    valueSource
                        .Subscribe(observer)
                };
            }));

            _onValueSet = _doNothingOnValueSet;
        }

        new public T Value
        {
            get => base.Value;
            set => _onValueSet.OnNext(value);
        }

        protected override event PropertyChangedEventHandler? PropertyChanged
        {
            add     => _propertyChanged.OnNext += value;
            remove  => _propertyChanged.OnNext -= value;
        }

        private readonly IPropertyChangedEventSource _propertyChanged;

        private IObserver<T> _onValueSet;

        private static readonly IObserver<T> _doNothingOnValueSet
            = Observer.Create<T>(static _ => { });
    }
}
