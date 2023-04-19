using System;
using System.Collections.Generic;
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
            _subscriptions              = new();
            _workspaceLaunchRequested   = new();

            var currentVersion = repository.CurrentVersions
                .Select(versions => versions.Single(version => version.AccountId == accountId))
                .DistinctUntilChanged()
                .ShareReplay(1);

            _children = repository.CurrentVersions
                .Select(versions => versions
                    .Where(version => version.ParentAccountId == accountId)
                    .OrderBy(version => version.Name)
                    .Select(version => version.AccountId)
                    .ToList())
                .ApplyOperation(accountIds => ViewAccountIds(accountIds, modelFactory))
                .ToReactiveProperty(Array.Empty<ListViewItemModel>());

            _createChildRequested = new();
            _createChildRequested
                .WithLatestFrom(currentVersion, (_, currentVersion) => currentVersion)
                .Subscribe(currentVersion => _workspaceLaunchRequested.OnNext(() => modelFactory
                    .CreateFormWorkspace(new CreationModel()
                    {
                        Name            = $"New {currentVersion.Type} Account",
                        ParentAccountId = currentVersion.AccountId,
                        Type            = currentVersion.Type
                    })))
                .DisposeWith(_subscriptions);

            _createChildCommand = ReactiveCommand.Create(_createChildRequested);

            _editRequested = new();
            _editRequested 
                .WithLatestFrom(currentVersion, (_, currentVersion) => currentVersion)
                .Subscribe(currentVersion => _workspaceLaunchRequested.OnNext(() => modelFactory
                    .CreateFormWorkspace(new MutationModel()
                    {
                        AccountId       = currentVersion.AccountId,
                        Description     = currentVersion.Description,
                        Name            = currentVersion.Name,
                        ParentAccountId = currentVersion.AccountId,
                        Type            = currentVersion.Type
                    })))
                .DisposeWith(_subscriptions);

            _editCommand = ReactiveCommand.Create(_editRequested);

            _name = currentVersion
                .Select(version => version.Name)
                .DistinctUntilChanged()
                .ToReactiveProperty(string.Empty);
        }

        public ListViewItemModel(
            ModelFactory        modelFactory,
            Repository          repository,
            Data.Accounts.Type  type)
        {
            _workspaceLaunchRequested = new();

            _children = repository.CurrentVersions
                .Select(versions => versions
                    .Where(version => (version.Type == type)
                        && (version.ParentAccountId is null))
                    .OrderBy(version => version.Name)
                    .Select(version => version.AccountId)
                    .ToList())
                .ApplyOperation(accountIds => ViewAccountIds(accountIds, modelFactory))
                .ToReactiveProperty(Array.Empty<ListViewItemModel>());

            _createChildCommand = ReactiveCommand.Create(() => _workspaceLaunchRequested.OnNext(() => modelFactory
                .CreateFormWorkspace(new CreationModel()
                {
                    Name    = $"New {type} Account",
                    Type    = type
                })));

            _editCommand = ReactiveCommand.NotSupported;

            _name = ReactiveProperty.Create(type.ToString());
        }

        public ReactiveCommand<Unit> CreateChildCommand
            => _createChildCommand;

        public ReactiveProperty<IReadOnlyList<ListViewItemModel>> Children
            => _children;

        public ReactiveCommand<Unit> EditCommand
            => _editCommand;

        public ReactiveProperty<string> Name
            => _name;

        public IObservable<Func<Workspaces.ModelBase>> WorkspaceLaunchRequested
            => _workspaceLaunchRequested;

        public void Dispose()
        {
            _createChildRequested?.OnCompleted();
            _editRequested?.OnCompleted();
            _subscriptions?.Dispose();
            _workspaceLaunchRequested.OnCompleted();
        }

        private static IObservable<IReadOnlyList<ListViewItemModel>> ViewAccountIds(
                IObservable<IReadOnlyList<ulong>>   accountIds,
                ModelFactory                        modelFactory)
            => accountIds
                .DistinctUntilChanged(accountIds => accountIds.AsEnumerable(), SequenceEqualityComparer<ulong>.Default)
                .Select(accountIds => (accountIds.Count is 0)
                    ? Observable.Return(Array.Empty<ListViewItemModel>())
                    : accountIds
                        .Select(accountId => ReactiveDisposable.Create(() => modelFactory.CreateListViewItem(accountId)))
                        .CombineLatest(children => children.ToArray()))
                .Switch();

        private readonly ReactiveProperty<IReadOnlyList<ListViewItemModel>> _children;
        private readonly ReactiveCommand<Unit>                              _createChildCommand;
        private readonly Subject<Unit>?                                     _createChildRequested;
        private readonly ReactiveCommand<Unit>                              _editCommand;
        private readonly Subject<Unit>?                                     _editRequested;
        private readonly ReactiveProperty<string>                           _name;
        private readonly CompositeDisposable?                               _subscriptions;
        private readonly Subject<Func<Workspaces.ModelBase>>                _workspaceLaunchRequested;
    }
}
