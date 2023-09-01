using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Saaft.Data
{
    public class StateStore<TEntity, TEvent>
            : DisposableBase,
                IObservable<TEntity>
        where TEntity : StateEntity<TEvent>
    {
        public StateStore(TEntity initialValue)
        {
            _valueSource = new(initialValue);

            _events = _valueSource
                .Select(static value => value.LatestEvent)
                .Skip(1)
                .Share();
        }

        public IObservable<TEvent> Events
            => _events;

        public TEntity Value
        {
            get => _valueSource.Value;
            set => _valueSource.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<TEntity> observer)
            => _valueSource.Subscribe(observer);

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
            {
                _valueSource.OnCompleted();

                _valueSource.Dispose();
            }
        }

        private readonly IObservable<TEvent>        _events;
        private readonly BehaviorSubject<TEntity>   _valueSource;
    }
}
