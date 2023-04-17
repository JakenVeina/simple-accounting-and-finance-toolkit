using System.Reactive.Disposables;

namespace System.Reactive.Subjects
{
    public sealed class ResettingReplaySubject<T>
        : SubjectBase<T>
    {
        public ResettingReplaySubject(int bufferSize)
            => _bufferSize = bufferSize;

        public override bool HasObservers
            => _source?.HasObservers ?? false;

        public override bool IsDisposed
            => _isDisposed;

        public override void Dispose()
        {
            if (!_isDisposed)
            {
                _source?.Dispose();
                _isDisposed = true;
            }
        }

        public override void OnCompleted()
        {
            if (_source is not null)
                _source.OnCompleted();
            else
                AssertNotDisposed();
        }

        public override void OnError(Exception error)
        {
            if (_source is not null)
                _source.OnError(error);
            else
                AssertNotDisposed();
        }

        public override void OnNext(T value)
        {
            if (_source is not null)
                _source.OnNext(value);
            else
                AssertNotDisposed();
        }

        public override IDisposable Subscribe(IObserver<T> observer)
        {
            if (_source is null)
            {
                AssertNotDisposed();

                _source = new ReplaySubject<T>(_bufferSize);
            }

            var subscription = _source.Subscribe(observer);
            return Disposable.Create(() =>
            {
                subscription.Dispose();
                if (_source is not null && !_source.HasObservers)
                    _source = null;
            });
        }

        private void AssertNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(string.Empty);
        }

        private readonly int _bufferSize;

        private bool                _isDisposed;
        private ReplaySubject<T>?   _source;
    }
}
