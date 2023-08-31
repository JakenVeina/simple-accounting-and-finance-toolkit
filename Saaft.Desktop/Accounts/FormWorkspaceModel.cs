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
        : HostedModelBase
    {
        public FormWorkspaceModel(
            Repository      repository,
            CreationModel   model)
        {
            _saveCommandExecuted    = new();
            _type                   = model.Type;

            _descriptionSource = new(model.Description);
            _description       = ReactiveProperty.Create(
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

            _name = ReactiveProperty.Create(
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
                .ToReactiveReadOnlyProperty();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: () => _saveCommandExecuted.OnNext(Unit.Default),
                canExecute: nameErrors
                    .Select(errors => errors.Length is 0));

            _title = ReactiveReadOnlyProperty.Create("Create New Account");

            _saveCompleted = _saveCommandExecuted
                .WithLatestFrom(_descriptionSource,         (_, description) => description)
                .WithLatestFrom(_nameSource.WhereNotNull(), (description, name) => (description, name))
                .Select(@params => model with
                {
                    Description = @params.description,
                    Name        = @params.name
                })
                .ApplyOperation(repository.Create)
                .SelectUnit()
                .Share();
        }

        public FormWorkspaceModel(
            Repository      repository,
            MutationModel   model)
        {
            _saveCommandExecuted    = new();
            _type                   = model.Type;

            _descriptionSource = new(model.Description);
            _description       = ReactiveProperty.Create(
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

            _name = ReactiveProperty.Create(
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
                .ToReactiveReadOnlyProperty();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: _saveCommandExecuted,
                canExecute: Observable.CombineLatest(
                    _descriptionSource.Select(description => description != model.Description),
                    _nameSource.Select(name => name != model.Name),
                    nameErrors,
                    static (isDescriptionDirty, isNameDirty, nameErrors) => (isDescriptionDirty || isNameDirty) && (nameErrors.Length is 0)));

            _title = ReactiveReadOnlyProperty.Create("Edit Account");

            _saveCompleted = _saveCommandExecuted
                .WithLatestFrom(_descriptionSource,         static (_, description) => description)
                .WithLatestFrom(_nameSource.WhereNotNull(), static (description, name) => (description, name))
                .Select(@params => model with
                {
                    Description = @params.description,
                    Name        = @params.name
                })
                .ApplyOperation(repository.Mutate)
                .SelectUnit()
                .Share();
        }

        public ReactiveProperty<string?> Description
            => _description;

        public ReactiveProperty<string?> Name
            => _name;

        public ReactiveReadOnlyProperty<string?> ParentName
            => _parentName;

        public ReactiveCommand SaveCommand
            => _saveCommand;

        public IObservable<Unit> SaveCompleted
            => _saveCompleted;

        public override ReactiveReadOnlyProperty<string> Title
            => _title;

        public Data.Accounts.Type Type
            => _type;

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
            {
                _descriptionSource.OnCompleted();
                _nameSource.OnCompleted();
                _saveCommandExecuted.OnCompleted();

                _descriptionSource.Dispose();
                _nameSource.Dispose();
                _saveCommandExecuted.Dispose();
            }
        }

        private readonly ReactiveProperty<string?>          _description;
        private readonly BehaviorSubject<string?>           _descriptionSource;
        private readonly ReactiveProperty<string?>          _name;
        private readonly BehaviorSubject<string?>           _nameSource;
        private readonly ReactiveReadOnlyProperty<string?>  _parentName;
        private readonly ReactiveCommand                    _saveCommand;
        private readonly Subject<Unit>                      _saveCommandExecuted;
        private readonly IObservable<Unit>                  _saveCompleted;
        private readonly ReactiveReadOnlyProperty<string>   _title;
        private readonly Data.Accounts.Type                 _type;
    }
}
