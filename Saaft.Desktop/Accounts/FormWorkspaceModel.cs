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
        : Workspaces.ModelBase,
            IDisposable
    {
        public FormWorkspaceModel(
            Repository      repository,
            CreationModel   model)
        {
            _description            = new(model.Description);
            _saveCommandExecuted    = new();
            _type                   = model.Type;

            _name = new(
                initialValue:   model.Name,
                validator:      name => Observable.CombineLatest(
                    name,
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
                    }));

            _parentName = ((model.ParentAccountId is ulong parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveProperty();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: _saveCommandExecuted,
                canExecute: _name.HasErrors
                    .Select(hasErrors => !hasErrors));

            _title = ReactiveProperty.Create("Create New Account");

            _saveCompleted = _saveCommandExecuted
                .WithLatestFrom(_description,           (_, description) => description)
                .WithLatestFrom(_name.WhereNotNull(),   (description, name) => (description, name))
                .Select(@params => model with
                {
                    Description = @params.description,
                    Name        = @params.name
                })
                .ApplyOperation(repository.Create)
                .Select(_ => Unit.Default)
                .Share();
        }

        public FormWorkspaceModel(
            Repository      repository,
            MutationModel   model)
        {
            _description            = new(model.Description);
            _saveCommandExecuted    = new();
            _type                   = model.Type;

            _name = new(
                initialValue:   model.Name,
                validator:      name => Observable.CombineLatest(
                    name,
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
                    }));

            _parentName = ((model.ParentAccountId is ulong parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveProperty();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: _saveCommandExecuted,
                canExecute: Observable.CombineLatest(
                    _description.Select(description => description != model.Description),
                    _name.Select(name => name != model.Name),
                    _name.HasErrors,
                    (isDescriptionDirty, isNameDirty, nameHasErrors) => (isDescriptionDirty || isNameDirty) && (!nameHasErrors)));

            _title = ReactiveProperty.Create("Edit Account");

            _saveCompleted = _saveCommandExecuted
                .WithLatestFrom(_description,           (_, description) => description)
                .WithLatestFrom(_name.WhereNotNull(),   (description, name) => (description, name))
                .Select(@params => model with
                {
                    Description = @params.description,
                    Name        = @params.name
                })
                .ApplyOperation(repository.Mutate)
                .Select(_ => Unit.Default)
                .Share();
        }

        public ObservableProperty<string?> Description
            => _description;

        public ObservableProperty<string?> Name
            => _name;

        public ReactiveProperty<string?> ParentName
            => _parentName;

        public ReactiveCommand SaveCommand
            => _saveCommand;

        public IObservable<Unit> SaveCompleted
            => _saveCompleted;

        public override ReactiveProperty<string> Title
            => _title;

        public Data.Accounts.Type Type
            => _type;

        public void Dispose()
        {
            _description.Dispose();
            _name.Dispose();
            _saveCommandExecuted.OnCompleted();
            _saveCommandExecuted.Dispose();
        }

        private readonly ObservableProperty<string?>    _description;
        private readonly ObservableProperty<string?>    _name;
        private readonly ReactiveProperty<string?>      _parentName;
        private readonly ReactiveCommand                _saveCommand;
        private readonly Subject<Unit>                  _saveCommandExecuted;
        private readonly IObservable<Unit>              _saveCompleted;
        private readonly ReactiveProperty<string>       _title;
        private readonly Data.Accounts.Type             _type;
    }
}
