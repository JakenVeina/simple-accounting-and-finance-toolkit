using System;
using System.Reactive.Subjects;

namespace Saaft.Desktop.Workspaces
{
    public abstract class PromptModelBase
        : DisposableBase
    {
        public abstract void Cancel();
    }

    public abstract class PromptModelBase<T>
        : PromptModelBase
    {
        protected PromptModelBase()
            => _result = new(1);

        public IObservable<T> Result
            => _result;

        public sealed override void Cancel()
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
