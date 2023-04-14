using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
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
            _description    = new(model.Description);
            _saveRequested  = new();
            _subscriptions  = new();
            _type           = model.Type;

            _name = new(
                initialValue:   model.Name,
                errorsFactory:  name => Observable.CombineLatest(
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

            _parentName = ((model.ParentAccountId is long parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveProperty();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: _saveRequested,
                canExecute: _name.HasErrors
                    .Select(hasErrors => !hasErrors));

            _title = ReactiveProperty.Create("Create New Account");

            _saveRequested
                .WithLatestFrom(_description,           (_, description) => description)
                .WithLatestFrom(_name.WhereNotNull(),   (description, name) => (description, name))
                .Select(@params => model with
                {
                    Description = @params.description,
                    Name        = @params.name
                })
                .ApplyOperation(repository.Create)
                .Subscribe()
                .DisposeWith(_subscriptions);
        }

        public FormWorkspaceModel(
            Repository      repository,
            MutationModel   model)
        {
            _description    = new(model.Description);
            _saveRequested  = new();
            _subscriptions  = new();
            _type           = model.Type;

            _name = new(
                initialValue:   model.Name,
                errorsFactory:  name => Observable.CombineLatest(
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

            _parentName = ((model.ParentAccountId is long parentAccountId)
                    ? repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId)
                            .Select(version => version.Name)
                            .FirstOrDefault())
                    : Observable.Return<string?>(null))
                .ToReactiveProperty();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: _saveRequested,
                canExecute: Observable.CombineLatest(
                    _description.Select(description => description != model.Description),
                    _name.Select(name => name != model.Name),
                    _name.HasErrors,
                    (isDescriptionDirty, isNameDirty, nameHasErrors) => (isDescriptionDirty || isNameDirty) && (!nameHasErrors)));

            _title = ReactiveProperty.Create("Edit Account");

            _saveRequested
                .WithLatestFrom(_description,           (_, description) => description)
                .WithLatestFrom(_name.WhereNotNull(),   (description, name) => (description, name))
                .Select(@params => model with
                {
                    Description = @params.description,
                    Name        = @params.name
                })
                .ApplyOperation(repository.Mutate)
                .Subscribe()
                .DisposeWith(_subscriptions);
        }

        public ObservableProperty<string?> Description
            => _description;

        public ObservableProperty<string?> Name
            => _name;

        public ReactiveProperty<string?> ParentName
            => _parentName;

        public ReactiveCommand<Unit> SaveCommand
            => _saveCommand;

        public override ReactiveProperty<string> Title
            => _title;

        public Data.Accounts.Type Type
            => _type;

        public void Dispose()
        {
            _description.Dispose();
            _name.Dispose();
            _saveRequested.Dispose();
            _subscriptions.Dispose();
        }

        private readonly ObservableProperty<string?>    _description;
        private readonly ObservableProperty<string?>    _name;
        private readonly ReactiveProperty<string?>      _parentName;
        private readonly ReactiveCommand<Unit>          _saveCommand;
        private readonly Subject<Unit>                  _saveRequested;
        private readonly CompositeDisposable            _subscriptions;
        private readonly ReactiveProperty<string>       _title;
        private readonly Data.Accounts.Type             _type;
    }
}
