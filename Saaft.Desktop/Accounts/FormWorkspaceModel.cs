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
        : IHostedModel,
            IDisposable
    {
        public FormWorkspaceModel(
            Repository      repository,
            CreationModel   model)
        {
            _closed = new();
            _type   = model.Type;

            _cancelCommand = ReactiveCommand.Create(_closed);

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
                errorSource:    nameErrors);

            _parentName = ((model.ParentAccountId is ulong parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveReadOnlyValue();

            _saveCommand = ReactiveCommand.Create(
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .WithLatestFrom(_descriptionSource,         (_, description) => description)
                    .WithLatestFrom(_nameSource.WhereNotNull(), (description, name) => (description, name))
                    .Select(@params => model with
                    {
                        Description = @params.description,
                        Name        = @params.name
                    })
                    .ApplyOperation(repository.Create)
                    .Do(_ => _closed.OnNext(Unit.Default))
                    .SelectUnit(),
                canExecute:         nameErrors
                    .Select(errors => errors.Length is 0));

            _title = ReactiveReadOnlyValue.Create("Create New Account");
        }

        public FormWorkspaceModel(
            Repository      repository,
            MutationModel   model)
        {
            _closed = new();
            _type   = model.Type;

            _cancelCommand = ReactiveCommand.Create(_closed);

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
                errorSource:    nameErrors);

            _parentName = ((model.ParentAccountId is ulong parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(static version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveReadOnlyValue();

            _saveCommand = ReactiveCommand.Create(
                executeOperation:   onExecuteRequested => onExecuteRequested
                    .WithLatestFrom(_descriptionSource,         static (_, description) => description)
                    .WithLatestFrom(_nameSource.WhereNotNull(), static (description, name) => (description, name))
                    .Select(@params => model with
                    {
                        Description = @params.description,
                        Name        = @params.name
                    })
                    .ApplyOperation(repository.Mutate)
                    .Do(_ => _closed.OnNext(Unit.Default))
                    .SelectUnit(),
                canExecute:         Observable.CombineLatest(
                    _descriptionSource.Select(description => description != model.Description),
                    _nameSource.Select(name => name != model.Name),
                    nameErrors,
                    static (isDescriptionDirty, isNameDirty, nameErrors) => (isDescriptionDirty || isNameDirty) && (nameErrors.Length is 0)));

            _title = ReactiveReadOnlyValue.Create("Edit Account");
        }

        public ReactiveCommand CancelCommand
            => _cancelCommand;

        public IObservable<Unit> Closed
            => _closed;

        public ReactiveValue<string?> Description
            => _description;

        public ReactiveValue<string?> Name
            => _name;

        public IObserver<Unit> OnCloseRequested
            => _closed;

        public ReactiveReadOnlyValue<string?> ParentName
            => _parentName;

        public ReactiveCommand SaveCommand
            => _saveCommand;

        public ReactiveReadOnlyValue<string> Title
            => _title;

        public Data.Accounts.Type Type
            => _type;

        public void Dispose()
        {
            _closed.OnCompleted();
            _descriptionSource.OnCompleted();
            _nameSource.OnCompleted();

            _closed.Dispose();
            _descriptionSource.Dispose();
            _nameSource.Dispose();
        }

        private readonly ReactiveCommand                _cancelCommand;
        private readonly Subject<Unit>                  _closed;
        private readonly ReactiveValue<string?>         _description;
        private readonly BehaviorSubject<string?>       _descriptionSource;
        private readonly ReactiveValue<string?>         _name;
        private readonly BehaviorSubject<string?>       _nameSource;
        private readonly ReactiveReadOnlyValue<string?> _parentName;
        private readonly ReactiveCommand                _saveCommand;
        private readonly ReactiveReadOnlyValue<string>  _title;
        private readonly Data.Accounts.Type             _type;
    }
}
