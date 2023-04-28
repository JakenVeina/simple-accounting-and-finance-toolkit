using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Saaft.Data
{
    public sealed class DataStateStore
        : IObservable<DataStateEntity>,
            IDisposable
    {
        public DataStateStore()
        {
            _valueSource = new(DataStateEntity.Default);

            _events = _valueSource
                .Select(static value => value.LatestEvent)
                .Skip(1)
                .Share();
        }

        public IObservable<DataStateEvent> Events
            => _events;

        public DataStateEntity Value
        {
            get => _valueSource.Value;
            set => _valueSource.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<DataStateEntity> observer)
            => _valueSource.Subscribe(observer);

        public void Dispose()
        {
            _valueSource.OnCompleted();
            _valueSource.Dispose();
        }

        private readonly IObservable<DataStateEvent>        _events;
        private readonly BehaviorSubject<DataStateEntity>   _valueSource;
    }
}
