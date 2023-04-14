using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Windows.Input;

using Saaft.Data;
using Saaft.Data.Accounts;
using Saaft.Desktop.Validation;

namespace Saaft.Desktop.Accounts
{
    public sealed class FormWorkspaceModel
        : Workspaces.ModelBase,
            IDisposable
    {
        public FormWorkspaceModel(
                    DataStore       dataStore,
                    Repository      repository,
                    CreationModel   model)
                : this(
                    dataStore:          dataStore,
                    repository:         repository,
                    parentAccountId:    model.ParentAccountId,
                    description:        model.Description,
                    name:               model.Name,
                    title:              ReactiveProperty.Create("Create New Account"),
                    type:               model.Type)
            => _saveRequested
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

        private FormWorkspaceModel(
            DataStore                   dataStore,
            Repository                  repository,
            long?                       parentAccountId,
            string?                     description,
            string?                     name,
            ReactiveProperty<string>    title,
            Data.Accounts.Type          type)
        {
            _description    = new(description);
            _subscriptions  = new();
            _title          = title;
            _type           = type;

            var versions = dataStore
                .WhereNotNull()
                .Select(file => file.Database.AccountVersions)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _name = new(
                initialValue:   name,
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

            _parentName = ((parentAccountId is null)
                    ? Observable.Return<string?>(null)
                    : repository.CurrentVersions
                        .Select(versions => versions
                            .Where(version => version.AccountId == parentAccountId.Value)
                            .Select(version => version.Name)
                            .FirstOrDefault()))
                .ToReactiveProperty();

            _saveRequested = new();

            _saveCommand = ReactiveCommand.Create(
                onExecuted: _saveRequested,
                canExecute: _name.HasErrors
                    .Select(hasErrors => !hasErrors));
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
