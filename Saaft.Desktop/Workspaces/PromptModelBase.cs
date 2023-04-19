using System;
using System.Reactive.Subjects;

namespace Saaft.Desktop.Workspaces
{
    public abstract class PromptModelBase<T>
        : DisposableBase
    {
        protected PromptModelBase()
            => _result = new(1);

        public IObservable<T> Result
            => _result;

        public void Cancel()
            => _result.OnCompleted();

        public void SetResult(T result)
        {
            _result.OnNext(result);
            _result.OnCompleted();
        }

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
                _result.Dispose();
        }

        private readonly ReplaySubject<T> _result;
    }
}
