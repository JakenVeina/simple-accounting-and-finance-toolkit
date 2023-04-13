using System;
using System.Reactive.Subjects;

using Saaft.Data.Database;

namespace Saaft.Data
{
    public sealed class DataStore
        : IObservable<FileEntity?>,
            IDisposable
    {
        public DataStore()
            => _valueSource = new(null);

        public FileEntity? Value
        {
            get => _valueSource.Value;
            set => _valueSource.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<FileEntity?> observer)
            => _valueSource.Subscribe(observer);

        public void Dispose()
            => _valueSource.Dispose();

        private readonly BehaviorSubject<FileEntity?> _valueSource;
    }
}
