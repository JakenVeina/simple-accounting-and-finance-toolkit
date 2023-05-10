using System;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace Saaft.Desktop.Prompts
{
    public class ImperativePromptModel<T>
        : HostedModelBase
    {
        public ImperativePromptModel(string title)
        {
            _result = new();
            
            _title = ReactiveReadOnlyProperty.Create(title);
        }

        public IObservable<T> Result
            => _result;

        public override ReactiveReadOnlyProperty<string> Title
            => _title;

        public void Cancel()
            => _result.OnCompleted();

        public void SetResult(T value)
        {
            _result.OnNext(value);
            _result.OnCompleted();
        }

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
                _result.Dispose();
        }

        private readonly ReplaySubject<T>                   _result;
        private readonly ReactiveReadOnlyProperty<string>   _title;
    }
}
