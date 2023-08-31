using System;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace Saaft.Desktop.Prompts
{
    public class ImperativePromptModel<T>
        : ModelBase<T>
    {
        public ImperativePromptModel(string title)
        {
            _result = new();
            
            _title = ReactiveReadOnlyProperty.Create(title);
        }

        public sealed override IObservable<T> Result
            => _result;

        public sealed override ReactiveReadOnlyProperty<string> Title
            => _title;

        public void Cancel()
            => _result.OnCompleted();

        public void PublishResult(T value)
        {
            _result.OnNext(value);
            _result.OnCompleted();
        }

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
                _result.OnCompleted();

            base.OnDisposing(type);

            if (type is DisposalType.Managed)
                _result.Dispose();
        }

        private readonly Subject<T>                         _result;
        private readonly ReactiveReadOnlyProperty<string>   _title;
    }
}
