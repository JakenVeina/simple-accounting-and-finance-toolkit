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

            _adoptAccountIdCommand = ReactiveCommand.Create(
                executeOperation:   onExecuteRequested => onExecuteRequested
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
                    .SelectUnit(),
                canExecute:         Observable.CombineLatest(currentVersion, repository.CurrentVersions, static (currentVersion, currentVersions) => (currentVersion, currentVersions))
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

            _children = repository.ObserveCurrentVersions(
                    filterPredicate:    version => version.ParentAccountId == accountId,
                    orderByClause:      static versions => versions.OrderBy(static version => version.Name),
                    selector:           version => ReactiveDisposable
                        .Create(() => modelFactory.CreateListViewItem(version.AccountId))
                        .ToReactiveReadOnlyValue())
                .ToReactiveCollection();

            _createChildCommand = ReactiveCommand.Create(
                executeOperation: onExecuteRequested => onExecuteRequested
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

            _editCommand = ReactiveCommand.Create(
                executeOperation: onExecuteRequested => onExecuteRequested
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

            _adoptAccountIdCommand = ReactiveCommand.Create<ulong>(
                executeOperation: onExecuteRequested => onExecuteRequested
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

            _createChildCommand = ReactiveCommand.Create(
                executeOperation: onExecuteRequested => onExecuteRequested
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

            _editCommand = ReactiveCommand.NotSupported;

            _name = ReactiveReadOnlyValue.Create(type.ToString());
        }

        public ulong? AccountId
            => _accountId;

        public ReactiveCommand AdoptAccountIdCommand
            => _adoptAccountIdCommand;

        public ReactiveCollection<ReactiveReadOnlyValue<ListViewItemModel?>> Children
            => _children;

        public ReactiveCommand CreateChildCommand
            => _createChildCommand;

        public ReactiveCommand EditCommand
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

                _hostRequested.Dispose();
            }
        }

        private readonly ulong?                                                         _accountId;
        private readonly ReactiveCommand                                                _adoptAccountIdCommand;
        private readonly ReactiveCollection<ReactiveReadOnlyValue<ListViewItemModel?>>  _children;
        private readonly ReactiveCommand                                                _createChildCommand;
        private readonly ReactiveCommand                                                _editCommand;
        private readonly Subject<IHostedModel>                                          _hostRequested;
        private readonly bool                                                           _isAccount;
        private readonly ReactiveReadOnlyValue<string>                                  _name;
    }
}
