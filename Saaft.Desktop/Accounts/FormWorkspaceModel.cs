using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

using Saaft.Data.Accounts;
using Saaft.Desktop.Validation;

namespace Saaft.Desktop.Accounts
{
    public sealed class FormWorkspaceModel
        : DisposableBase,
            IHostedModel
    {
        public FormWorkspaceModel(
            Repository      repository,
            CreationModel   model)
        {
            _closed = new();
            _type   = model.Type;

            _cancelCommand = ReactiveActionCommand.Create(onExecuteRequested: Close);

            _descriptionSource = new(model.Description);
            _description       = ReactiveValue.Create(
                initialValue:   _descriptionSource.Value,
                valueSource:    _descriptionSource);

            _nameSource = new(model.Name);
            var nameErrors = Observable.CombineLatest(
                _nameSource,
                repository.CurrentVersions,
                (name, versions) => name switch
                {
                    _ when string.IsNullOrWhiteSpace(name)
                        => new[] { ValueIsRequiredError.Default },
                    _ when versions
                            .Select(version => version.Name)
                            .Contains(name)
                        => new[] { new NameExistsError() { Name = name } },
                    _   => Array.Empty<object?>()
                })
                .ShareReplay(1);

            _name = ReactiveValue.Create(
                initialValue:   _nameSource.Value,
                valueSource:    _nameSource,
                errorsSource:   nameErrors);

            _onCloseRequested = Observer.Create<Unit>(
                onNext:         unit =>
                {
                    _closed.OnNext(unit);
                    _closed.OnCompleted();
                },
                onError:        _closed.OnError,
                onCompleted:    _closed.OnCompleted);

            _parentName = ((model.ParentAccountId is ulong parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveReadOnlyValue();

            _saveCommand = ReactiveActionCommand.Create(
                canExecute:         nameErrors
                    .Select(errors => errors.Length is 0),
                executeOperation:   executeRequested => executeRequested
                    .WithLatestFrom(_descriptionSource,         (_, description) => description)
                    .WithLatestFrom(_nameSource.WhereNotNull(), (description, name) => (description, name))
                    .Select(@params => model with
                    {
                        Description = @params.description,
                        Name        = @params.name
                    })
                    .ApplyOperation(repository.Create)
                    .Do(_ => _closed.OnNext(Unit.Default))
                    .SelectUnit());

            _title = ReactiveReadOnlyValue.Create("Create New Account");
        }

        public FormWorkspaceModel(
            Repository      repository,
            MutationModel   model)
        {
            _closed = new();
            _type   = model.Type;

            _cancelCommand = ReactiveActionCommand.Create(onExecuteRequested: Close);

            _descriptionSource = new(model.Description);
            _description       = ReactiveValue.Create(
                initialValue:   _descriptionSource.Value,
                valueSource:    _descriptionSource);

            _nameSource = new(model.Name);
            var nameErrors = Observable.CombineLatest(
                _nameSource,
                repository.CurrentVersions,
                (name, versions) => name switch
                {
                    _ when string.IsNullOrWhiteSpace(name)
                        => new[] { ValueIsRequiredError.Default },
                    _ when versions
                            .Where(version => version.AccountId != model.AccountId)
                            .Select(version => version.Name)
                            .Contains(name)
                        => new[] { new NameExistsError() { Name = name } },
                    _   => Array.Empty<object?>()
                })
                .ShareReplay(1);

            _name = ReactiveValue.Create(
                initialValue:   _nameSource.Value,
                valueSource:    _nameSource,
                errorsSource:   nameErrors);

            _onCloseRequested = Observer.Create<Unit>(
                onNext:         unit =>
                {
                    _closed.OnNext(unit);
                    _closed.OnCompleted();
                },
                onError:        _closed.OnError,
                onCompleted:    _closed.OnCompleted);

            _parentName = ((model.ParentAccountId is ulong parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(static version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveReadOnlyValue();

            _saveCommand = ReactiveActionCommand.Create(
                canExecute:         Observable.CombineLatest(
                    _descriptionSource.Select(description => description != model.Description),
                    _nameSource.Select(name => name != model.Name),
                    nameErrors,
                    static (isDescriptionDirty, isNameDirty, nameErrors) => (isDescriptionDirty || isNameDirty) && (nameErrors.Length is 0)),
                executeOperation:   executeRequested => executeRequested
                    .WithLatestFrom(_descriptionSource,         static (_, description) => description)
                    .WithLatestFrom(_nameSource.WhereNotNull(), static (description, name) => (description, name))
                    .Select(@params => model with
                    {
                        Description = @params.description,
                        Name        = @params.name
                    })
                    .ApplyOperation(repository.Mutate)
                    .Do(_ => _closed.OnNext(Unit.Default))
                    .SelectUnit());

            _title = ReactiveReadOnlyValue.Create("Edit Account");
        }

        public IReactiveActionCommand CancelCommand
            => _cancelCommand;

        public IObservable<Unit> Closed
            => _closed;

        public ReactiveValue<string?> Description
            => _description;

        public ReactiveValue<string?> Name
            => _name;

        public IObserver<Unit> OnCloseRequested
            => _onCloseRequested;

        public ReactiveReadOnlyValue<string?> ParentName
            => _parentName;

        public IReactiveActionCommand SaveCommand
            => _saveCommand;

        public ReactiveReadOnlyValue<string> Title
            => _title;

        public Data.Accounts.Type Type
            => _type;

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
            {
                _closed             .OnCompleted();
                _descriptionSource  .OnCompleted();
                _nameSource         .OnCompleted();

                _cancelCommand      .Dispose();
                _closed             .Dispose();
                _descriptionSource  .Dispose();
                _nameSource         .Dispose();
                _saveCommand        .Dispose();
            }
        }

        private void Close()
        {
            _closed.OnNext(Unit.Default);
            _closed.OnCompleted();
        }

        private readonly ReactiveActionCommand          _cancelCommand;
        private readonly Subject<Unit>                  _closed;
        private readonly ReactiveValue<string?>         _description;
        private readonly BehaviorSubject<string?>       _descriptionSource;
        private readonly ReactiveValue<string?>         _name;
        private readonly BehaviorSubject<string?>       _nameSource;
        private readonly IObserver<Unit>                _onCloseRequested;
        private readonly ReactiveReadOnlyValue<string?> _parentName;
        private readonly ReactiveActionCommand          _saveCommand;
        private readonly ReactiveReadOnlyValue<string>  _title;
        private readonly Data.Accounts.Type             _type;
    }
}
