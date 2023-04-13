using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;

using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public sealed class ListViewItemModel
        : IDisposable
    {
        public ListViewItemModel(
            DataStore                   dataStore,
            ListViewItemModelFactory    itemFactory,
            FormWorkspaceModelFactory   formWorkspaceFactory,
            long                        accountId)
        {
            _subscriptions = new();

            _interruptRequested = new();

            var currentVersion = dataStore
                .Select(file => file?.Database.AccountVersions ?? ImmutableList<VersionEntity>.Empty)
                .DistinctUntilChanged()
                .Select(versions => versions
                    .Where(version => versions
                        .All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                    .Single(version => version.AccountId == accountId))
                .DistinctUntilChanged()
                .ShareReplay(1);

            _children = dataStore
                .Select(file => file?.Database.AccountVersions ?? ImmutableList<VersionEntity>.Empty)
                .DistinctUntilChanged()
                .Select(versions => versions
                    .Where(version => (version.ParentAccountId == accountId)
                        && versions.All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                    .OrderBy(version => version.Name)
                    .Select(version => version.AccountId)
                    .ToList())
                .DistinctUntilChanged(SequenceEqualityComparer<long>.Default)
                .Select(accountIds => accountIds
                    .Select(itemFactory.Create)
                    .ToList())
                .ToReactiveProperty(Array.Empty<ListViewItemModel>().AsReadOnlyList());

            _createChildRequested = new();
            _createChildRequested
                .WithLatestFrom(currentVersion, (_, currentVersion) => currentVersion)
                .Subscribe(currentVersion => _interruptRequested.OnNext(formWorkspaceFactory.Create(new()
                {
                    Name            = $"New {currentVersion.Type} Account",
                    ParentAccountId = currentVersion.AccountId,
                    Type            = currentVersion.Type
                })))
                .DisposeWith(_subscriptions);

            _createChildCommand = ReactiveCommand.Create(_createChildRequested);

            _name = currentVersion
                .Select(version => version.Name)
                .DistinctUntilChanged()
                .ToReactiveProperty(string.Empty);
        }

        public ListViewItemModel(
            DataStore                   dataStore,
            ListViewItemModelFactory    itemViewFactory,
            FormWorkspaceModelFactory   formWorkspaceFactory,
            Data.Accounts.Type          type)
        {
            _interruptRequested = new();

            _children = dataStore
                .Select(file => file?.Database.AccountVersions ?? ImmutableList<VersionEntity>.Empty)
                .DistinctUntilChanged()
                .Select(versions => versions
                    .Where(version => (version.Type == type)
                        && versions.All(nextVersion => nextVersion.PreviousVersionId != version.Id))
                    .OrderBy(version => version.Name)
                    .Select(version => version.AccountId)
                    .ToList())
                .DistinctUntilChanged(SequenceEqualityComparer<long>.Default)
                .Select(accountIds => accountIds
                    .Select(itemViewFactory.Create)
                    .ToList())
                .ToReactiveProperty(Array.Empty<ListViewItemModel>().AsReadOnlyList());

            _createChildCommand = ReactiveCommand.Create(() => _interruptRequested.OnNext(formWorkspaceFactory.Create(new()
            {
                Name    = $"New {type} Account",
                Type    = type
            })));

            _name = ReactiveProperty.Create(type.ToString());
        }

        public ReactiveCommand<Unit> CreateChildCommand
            => _createChildCommand;

        public ReactiveProperty<IReadOnlyList<ListViewItemModel>> Children
            => _children;

        public IObservable<Workspaces.ModelBase> InterruptRequested
            => _interruptRequested;

        public ReactiveProperty<string> Name
            => _name;

        public void Dispose()
        {
            _createChildRequested?.Dispose();
            _interruptRequested.Dispose();
            _subscriptions?.Dispose();
        }

        private readonly ReactiveProperty<IReadOnlyList<ListViewItemModel>> _children;
        private readonly ReactiveCommand<Unit>                              _createChildCommand;
        private readonly Subject<Unit>?                                     _createChildRequested;
        private readonly Subject<Workspaces.ModelBase>                      _interruptRequested;
        private readonly ReactiveProperty<string>                           _name;
        private readonly CompositeDisposable?                               _subscriptions;
    }
}
