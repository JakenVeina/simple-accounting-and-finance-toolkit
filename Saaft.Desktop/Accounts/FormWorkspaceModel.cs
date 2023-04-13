using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
                CreationModel   initialState)
            : this(
                dataStore:          dataStore,
                accountId:          null,
                parentAccountId:    initialState.ParentAccountId,
                description:        initialState.Description,
                name:               initialState.Name,
                type:               initialState.Type)
        { }

        private FormWorkspaceModel(
            DataStore           dataStore,
            long?               accountId,
            long?               parentAccountId,
            string?             description,
            string?             name,
            Data.Accounts.Type  type)
        {
            _accountId      = accountId;
            _description    = new(description);
            _type           = type;

            var versions = dataStore
                .WhereNotNull()
                .Select(file => file.Database.AccountVersions)
                .DistinctUntilChanged()
                .ShareReplay(1);

            _name = new(
                initialValue:   name,
                errorsFactory:  name => Observable.CombineLatest(name, versions, (name, versions) => name switch
                {
                    _ when string.IsNullOrWhiteSpace(name)
                        => new[] { ValueIsRequiredError.Default },
                    _ when versions
                            .Where(version => versions.All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                            .Select(version => version.Name)
                            .Contains(name)
                        => new[] { new NameExistsError() { Name = name! } },
                    _   => Array.Empty<object?>()
                }));

            _parentName = ((parentAccountId is null)
                    ? Observable.Return<string?>(null)
                    : versions
                        .Select(versions => versions
                            .FirstOrDefault(version => (version.AccountId == parentAccountId.Value)
                                && versions.All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                            ?.Name))
                .ToReactiveProperty();

            _saveRequested = new();
            _saveCommand = ReactiveCommand.Create(
                onExecuted: Observer.Create<Unit>(_ => 
                {
                    var previousVersion = (_accountId is long accountid)
                        ? dataStore.Value!.Database.AccountVersions.Find(version => (version.AccountId == accountid)
                            && dataStore.Value.Database.AccountVersions.All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                        : null;

                    dataStore.Value = dataStore.Value! with
                    {
                        Database = dataStore.Value.Database with
                        {
                            AccountVersions = (previousVersion is null)
                                ? dataStore.Value.Database.AccountVersions
                                    .Add(new()
                                    {
                                        AccountId           = dataStore.Value.Database.AccountVersions
                                            .DefaultIfEmpty()
                                            .Max(version => version?.AccountId ?? 0) + 1,
                                        //CreationId
                                        Description         = _description.Value!,
                                        Id                  = dataStore.Value.Database.AccountVersions
                                            .DefaultIfEmpty()
                                            .Max(version => version?.Id ?? 0) + 1,
                                        Name                = _name.Value!,
                                        ParentAccountId     = parentAccountId,
                                        Type                = _type
                                    })
                                : dataStore.Value.Database.AccountVersions
                                    .Replace(previousVersion, previousVersion with
                                    {
                                        //CreationId
                                        Description         = _description.Value!,
                                        Id                  = dataStore.Value.Database.AccountVersions
                                            .DefaultIfEmpty()
                                            .Max(version => version?.Id ?? 0) + 1,
                                        Name                = _name.Value!,
                                        PreviousVersionId   = previousVersion.Id,
                                    })
                        },
                    };
                }),
                canExecute: _name.HasErrors
                    .Select(hasErrors => !hasErrors));

            _title = Observable.Empty<string>()
                .ToReactiveProperty((accountId is null)
                    ? "Create New Account"
                    : "Edit Account");
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
        }

        private readonly long?                          _accountId;
        private readonly ObservableProperty<string?>    _description;
        private readonly ObservableProperty<string?>    _name;
        private readonly ReactiveProperty<string?>      _parentName;
        private readonly ReactiveCommand<Unit>          _saveCommand;
        private readonly Subject<Unit>                  _saveRequested;
        private readonly ReactiveProperty<string>       _title;
        private readonly Data.Accounts.Type             _type;
    }
}
