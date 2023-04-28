using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public sealed class ListViewItemModel
        : IDisposable
    {
        public ListViewItemModel(
            ModelFactory    modelFactory,
            Repository      repository,
            ulong           accountId)
        {
            _accountId                      = accountId;
            _adoptAccountIdCommandExecuted  = new();
            _createChildCommandExecuted     = new();
            _isAccount                      = true;
            _subscriptions                  = new();
            _workspaceLaunchRequested       = new();

            var currentVersion = repository.CurrentVersions
                .Select(versions => versions.SingleOrDefault(version => version.AccountId == accountId))
                .WhereNotNull()
                .DistinctUntilChanged()
                .ShareReplay(1);

            _adoptAccountIdCommand = ReactiveCommand.Create(
                onExecuted: _adoptAccountIdCommandExecuted,
                canExecute: Observable.CombineLatest(currentVersion, repository.CurrentVersions, static (currentVersion, currentVersions) => (currentVersion, currentVersions))
                    .Select(static @params =>
                    {
                        var invalidNewChildAccountIds = new HashSet<ulong>(@params.currentVersions.Count)
                        {
                            // Account cannot be its own child
                            @params.currentVersion.AccountId
                        };

                        // Ignore existing children, to avoid redundant updates
                        foreach(var version in @params.currentVersions.Where(version => version.ParentAccountId == @params.currentVersion.AccountId))
                            invalidNewChildAccountIds.Add(version.AccountId);

                        // Account cannot adopt an ancestor, that would cause a loop.
                        var ancestorAccountId = @params.currentVersion.ParentAccountId;
                        while(ancestorAccountId is not null)
                        {
                            invalidNewChildAccountIds.Add(ancestorAccountId.Value);
                            ancestorAccountId = @params.currentVersions
                                .FirstOrDefault(version => version.AccountId == ancestorAccountId.Value)
                                ?.ParentAccountId;
                        }

                        return new Predicate<ulong?>(newChildAccountId => (newChildAccountId is null)
                            || !invalidNewChildAccountIds.Contains(newChildAccountId.Value));
                    }));

            _adoptAccountIdCommandExecuted
                .WithLatestFrom(repository.CurrentVersions, static (targetAccountId, currentVersions) => currentVersions
                    .First(version => version.AccountId == targetAccountId))
                .WithLatestFrom(currentVersion, static (targetVersion, currentVersion) => new MutationModel()
                {
                    AccountId       = targetVersion.AccountId,
                    Description     = targetVersion.Description,
                    Name            = targetVersion.Name,
                    ParentAccountId = currentVersion.AccountId,
                    Type            = currentVersion.Type
                })
                .ApplyOperation(repository.Mutate)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _children = repository.ObserveCurrentVersions(
                    filterPredicate:    version => version.ParentAccountId == accountId,
                    orderByClause:      static versions => versions.OrderBy(static version => version.Name),
                    selector:           version => ReactiveDisposable
                        .Create(() => modelFactory.CreateListViewItem(version.AccountId))
                        .ToReactiveReadOnlyProperty())
                .ToReactiveCollection();

            _createChildCommandExecuted
                .WithLatestFrom(currentVersion, static (_, currentVersion) => currentVersion)
                .Subscribe(currentVersion => _workspaceLaunchRequested.OnNext(() => modelFactory
                    .CreateFormWorkspace(new CreationModel()
                    {
                        Name            = $"New {currentVersion.Type} Account",
                        ParentAccountId = currentVersion.AccountId,
                        Type            = currentVersion.Type
                    })))
                .DisposeWith(_subscriptions);

            _createChildCommand = ReactiveCommand.Create(_createChildCommandExecuted);

            _editCommandExecuted = new();
            _editCommandExecuted 
                .WithLatestFrom(currentVersion, static (_, currentVersion) => currentVersion)
                .Subscribe(currentVersion => _workspaceLaunchRequested.OnNext(() => modelFactory
                    .CreateFormWorkspace(new MutationModel()
                    {
                        AccountId       = currentVersion.AccountId,
                        Description     = currentVersion.Description,
                        Name            = currentVersion.Name,
                        ParentAccountId = currentVersion.ParentAccountId,
                        Type            = currentVersion.Type
                    })))
                .DisposeWith(_subscriptions);

            _editCommand = ReactiveCommand.Create(_editCommandExecuted);

            _name = currentVersion
                .Select(static version => version.Name)
                .DistinctUntilChanged()
                .ToReactiveReadOnlyProperty(string.Empty);
        }

        public ListViewItemModel(
            ModelFactory        modelFactory,
            Repository          repository,
            Data.Accounts.Type  type)
        {
            _adoptAccountIdCommandExecuted  = new();
            _isAccount                      = false;
            _subscriptions                  = new();
            _workspaceLaunchRequested       = new();

            _adoptAccountIdCommand = ReactiveCommand.Create(
                onExecuted: _adoptAccountIdCommandExecuted);

            _adoptAccountIdCommandExecuted
                .WithLatestFrom(repository.CurrentVersions, static (targetAccountId, currentVersions) => currentVersions
                    .First(version => version.AccountId == targetAccountId))
                .Select(targetVersion => new MutationModel()
                {
                    AccountId       = targetVersion.AccountId,
                    Description     = targetVersion.Description,
                    Name            = targetVersion.Name,
                    ParentAccountId = null,
                    Type            = type
                })
                .ApplyOperation(repository.Mutate)
                .Subscribe()
                .DisposeWith(_subscriptions);

            _children = repository.ObserveCurrentVersions(
                    filterPredicate:    version => (version.ParentAccountId is null)
                        && (version.Type == type),
                    orderByClause:      static versions => versions.OrderBy(static version => version.Name),
                    selector:           version => ReactiveDisposable
                        .Create(() => modelFactory.CreateListViewItem(version.AccountId))
                        .ToReactiveReadOnlyProperty())
                .ToReactiveCollection();

            _createChildCommand = ReactiveCommand.Create(() => _workspaceLaunchRequested.OnNext(() => modelFactory
                .CreateFormWorkspace(new CreationModel()
                {
                    Name    = $"New {type} Account",
                    Type    = type
                })));

            _editCommand = ReactiveCommand.NotSupported;

            _name = ReactiveReadOnlyProperty.Create(type.ToString());
        }

        public ulong? AccountId
            => _accountId;

        public ReactiveCommand AdoptAccountIdCommand
            => _adoptAccountIdCommand;

        public ReactiveCollection<ReactiveReadOnlyProperty<ListViewItemModel?>> Children
            => _children;

        public ReactiveCommand CreateChildCommand
            => _createChildCommand;

        public ReactiveCommand EditCommand
            => _editCommand;

        public bool IsAccount
            => _isAccount;

        public ReactiveReadOnlyProperty<string> Name
            => _name;

        public IObservable<Func<Workspaces.ModelBase>> WorkspaceLaunchRequested
            => _workspaceLaunchRequested;

        public void Dispose()
        {
            _adoptAccountIdCommandExecuted.OnCompleted();
            _adoptAccountIdCommandExecuted.Dispose();
            if (_createChildCommandExecuted is not null)
            {
                _createChildCommandExecuted.OnCompleted();
                _createChildCommandExecuted.Dispose();
            }
            if (_editCommandExecuted is not null)
            {
                _editCommandExecuted.OnCompleted();
                _editCommandExecuted.Dispose();
            }
            _subscriptions.Dispose();
            _workspaceLaunchRequested.OnCompleted();
            _workspaceLaunchRequested.Dispose();
        }

        private readonly ulong?                                                             _accountId;
        private readonly ReactiveCommand                                                    _adoptAccountIdCommand;
        private readonly Subject<ulong>                                                     _adoptAccountIdCommandExecuted;
        private readonly ReactiveCollection<ReactiveReadOnlyProperty<ListViewItemModel?>>   _children;
        private readonly ReactiveCommand                                                    _createChildCommand;
        private readonly Subject<Unit>?                                                     _createChildCommandExecuted;
        private readonly ReactiveCommand                                                    _editCommand;
        private readonly Subject<Unit>?                                                     _editCommandExecuted;
        private readonly bool                                                               _isAccount;
        private readonly ReactiveReadOnlyProperty<string>                                   _name;
        private readonly CompositeDisposable                                                _subscriptions;
        private readonly Subject<Func<Workspaces.ModelBase>>                                _workspaceLaunchRequested;
    }
}
