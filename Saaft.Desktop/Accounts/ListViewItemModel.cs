using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public sealed class ListViewItemModel
        : DisposableBase
    {
        public ListViewItemModel(
            ModelFactory    modelFactory,
            Repository      repository,
            ulong           accountId)
        {
            _accountId      = accountId;
            _hostRequested  = new();
            _isAccount      = true;

            var currentVersion = repository.CurrentVersions
                .Select(versions => versions.SingleOrDefault(version => version.AccountId == accountId))
                .WhereNotNull()
                .DistinctUntilChanged()
                .ShareReplay(1);

            _adoptAccountIdCommand = ReactiveValueCommand<ulong>.Create(
                canExecuteState:    Observable.CombineLatest(
                        currentVersion,
                        repository.CurrentVersions,
                        static (currentVersion, currentVersions) => (currentVersion, currentVersions))
                    .Select(static @params =>
                    {
                        var unadoptableAccountIds = new HashSet<ulong>(@params.currentVersions.Count)
                        {
                            // Account cannot be its own child
                            @params.currentVersion.AccountId
                        };

                        // Ignore existing children, to avoid redundant updates
                        foreach(var version in @params.currentVersions.Where(version => version.ParentAccountId == @params.currentVersion.AccountId))
                            unadoptableAccountIds.Add(version.AccountId);

                        // Account cannot adopt an ancestor, that would cause a loop.
                        var ancestorAccountId = @params.currentVersion.ParentAccountId;
                        while(ancestorAccountId is not null)
                        {
                            unadoptableAccountIds.Add(ancestorAccountId.Value);
                            ancestorAccountId = @params.currentVersions
                                .FirstOrDefault(version => version.AccountId == ancestorAccountId.Value)
                                ?.ParentAccountId;
                        }

                        return unadoptableAccountIds;
                    }),
                canExecute:         static (unadoptableAccountIds, accountIdToAdopt) => !unadoptableAccountIds.Contains(accountIdToAdopt),
                executeOperation:   executeRequested => executeRequested
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
                    .SelectUnit());

            _children = repository.ObserveCurrentVersions(
                    filterPredicate:    version => version.ParentAccountId == accountId,
                    orderByClause:      static versions => versions.OrderBy(static version => version.Name),
                    selector:           version => ReactiveDisposable
                        .Create(() => modelFactory.CreateListViewItem(version.AccountId))
                        .ToReactiveReadOnlyValue())
                .ToReactiveCollection();

            _createChildCommand = ReactiveActionCommand.Create(
                executeOperation: executeRequested => executeRequested
                    .WithLatestFrom(currentVersion, static (_, currentVersion) => currentVersion)
                    .Select(currentVersion => ReactiveDisposable
                        .Create(() => modelFactory.CreateFormWorkspace(new CreationModel()
                        {
                            Name            = $"New {currentVersion.Type} Account",
                            ParentAccountId = currentVersion.AccountId,
                            Type            = currentVersion.Type
                        }))
                        .Select(formWorkspace => formWorkspace.Closed
                            .OnSubscribed(() => _hostRequested.OnNext(formWorkspace)))
                        .Switch())
                    .Switch()
                    .SelectUnit());

            _editCommand = ReactiveActionCommand.Create(
                executeOperation: executeRequested => executeRequested
                    .WithLatestFrom(currentVersion, static (_, currentVersion) => currentVersion)
                    .Select(currentVersion => ReactiveDisposable
                        .Create(() => modelFactory.CreateFormWorkspace(new MutationModel()
                        {
                            AccountId       = currentVersion.AccountId,
                            Description     = currentVersion.Description,
                            Name            = currentVersion.Name,
                            ParentAccountId = currentVersion.ParentAccountId,
                            Type            = currentVersion.Type
                        }))
                        .Select(formWorkspace => formWorkspace.Closed
                            .OnSubscribed(() => _hostRequested.OnNext(formWorkspace)))
                        .Switch())
                    .Switch()
                    .SelectUnit());

            _name = currentVersion
                .Select(static version => version.Name)
                .DistinctUntilChanged()
                .ToReactiveReadOnlyValue(string.Empty);
        }

        public ListViewItemModel(
            ModelFactory        modelFactory,
            Repository          repository,
            Data.Accounts.Type  type)
        {
            _hostRequested  = new();
            _isAccount      = false;

            _adoptAccountIdCommand = ReactiveValueCommand<ulong>.Create(
                executeOperation: executeRequested => executeRequested
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
                    .SelectUnit());

            _children = repository.ObserveCurrentVersions(
                    filterPredicate:    version => (version.ParentAccountId is null)
                        && (version.Type == type),
                    orderByClause:      static versions => versions.OrderBy(static version => version.Name),
                    selector:           version => ReactiveDisposable
                        .Create(() => modelFactory.CreateListViewItem(version.AccountId))
                        .ToReactiveReadOnlyValue())
                .ToReactiveCollection();

            _createChildCommand = ReactiveActionCommand.Create(
                executeOperation: executeRequested => executeRequested
                    .Select(_ => ReactiveDisposable
                        .Create(() => modelFactory.CreateFormWorkspace(new CreationModel()
                        {
                            Name    = $"New {type} Account",
                            Type    = type
                        }))
                        .Select(formWorkspace => formWorkspace.Closed
                            .OnSubscribed(() => _hostRequested.OnNext(formWorkspace)))
                        .Switch())
                    .Switch()
                    .SelectUnit());

            _editCommand = ReactiveActionCommand.Create(
                canExecute:         Observable.Return(false),
                executeOperation:   static executed => executed);

            _name = ReactiveReadOnlyValue.Create(type.ToString());
        }

        public ulong? AccountId
            => _accountId;

        public IReactiveValueCommand<ulong> AdoptAccountIdCommand
            => _adoptAccountIdCommand;

        public ReactiveCollection<ReactiveReadOnlyValue<ListViewItemModel?>> Children
            => _children;

        public IReactiveActionCommand CreateChildCommand
            => _createChildCommand;

        public IReactiveActionCommand EditCommand
            => _editCommand;

        public IObservable<IHostedModel> HostRequested
            => _hostRequested;

        public bool IsAccount
            => _isAccount;

        public ReactiveReadOnlyValue<string> Name
            => _name;

        protected override void OnDisposing(DisposalType type)
        {
            if (type is DisposalType.Managed)
            {
                _hostRequested.OnCompleted();

                _adoptAccountIdCommand  .Dispose();
                _createChildCommand     .Dispose();
                _editCommand            .Dispose();
                _hostRequested          .Dispose();
            }
        }

        private readonly ulong?                                                         _accountId;
        private readonly ReactiveValueCommand<ulong>                                    _adoptAccountIdCommand;
        private readonly ReactiveCollection<ReactiveReadOnlyValue<ListViewItemModel?>>  _children;
        private readonly ReactiveActionCommand                                          _createChildCommand;
        private readonly ReactiveActionCommand                                          _editCommand;
        private readonly Subject<IHostedModel>                                          _hostRequested;
        private readonly bool                                                           _isAccount;
        private readonly ReactiveReadOnlyValue<string>                                  _name;
    }
}
