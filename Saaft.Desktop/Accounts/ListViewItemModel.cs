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
            FormWorkspaceModelFactory   formWorkspaceFactory,
            ListViewItemModelFactory    itemFactory,
            Repository                  repository,
            long                        accountId)
        {
            _subscriptions = new();

            _interruptRequested = new();

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
                .DistinctUntilChanged(SequenceEqualityComparer<long>.Default)
                .Select(accountIds => accountIds
                    .Select(itemFactory.Create)
                    .ToList())
                .ToReactiveProperty(Array.Empty<ListViewItemModel>().AsReadOnlyList());

            _createChildRequested = new();
            _createChildRequested
                .WithLatestFrom(currentVersion, (_, currentVersion) => currentVersion)
                .Subscribe(currentVersion => _interruptRequested.OnNext(formWorkspaceFactory.Create(new CreationModel()
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
                .Subscribe(currentVersion => _interruptRequested.OnNext(formWorkspaceFactory.Create(new MutationModel()
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
            FormWorkspaceModelFactory   formWorkspaceFactory,
            ListViewItemModelFactory    itemFactory,
            Repository                  repository,
            Data.Accounts.Type          type)
        {
            _interruptRequested = new();

            _children = repository.CurrentVersions
                .Select(versions => versions
                    .Where(version => (version.Type == type)
                        && (version.ParentAccountId is null))
                    .OrderBy(version => version.Name)
                    .Select(version => version.AccountId)
                    .ToList())
                .DistinctUntilChanged(SequenceEqualityComparer<long>.Default)
                .Select(accountIds => accountIds
                    .Select(itemFactory.Create)
                    .ToList())
                .ToReactiveProperty(Array.Empty<ListViewItemModel>().AsReadOnlyList());

            _createChildCommand = ReactiveCommand.Create(() => _interruptRequested.OnNext(formWorkspaceFactory.Create(new CreationModel()
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

        public IObservable<Workspaces.ModelBase> InterruptRequested
            => _interruptRequested;

        public ReactiveProperty<string> Name
            => _name;

        public void Dispose()
        {
            _createChildRequested?.Dispose();
            _editRequested?.Dispose();
            _interruptRequested.Dispose();
            _subscriptions?.Dispose();
        }

        private readonly ReactiveProperty<IReadOnlyList<ListViewItemModel>> _children;
        private readonly ReactiveCommand<Unit>                              _createChildCommand;
        private readonly Subject<Unit>?                                     _createChildRequested;
        private readonly ReactiveCommand<Unit>                              _editCommand;
        private readonly Subject<Unit>?                                     _editRequested;
        private readonly Subject<Workspaces.ModelBase>                      _interruptRequested;
        private readonly ReactiveProperty<string>                           _name;
        private readonly CompositeDisposable?                               _subscriptions;
    }
}
