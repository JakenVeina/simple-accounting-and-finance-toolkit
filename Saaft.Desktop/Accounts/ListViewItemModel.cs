using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class ListViewItemModel
    {
        public ListViewItemModel(
            DataStore                   dataStore,
            ListViewItemModelFactory    itemFactory,
            long                        accountId)
        {
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

            _name = currentVersion
                .Select(version => version.Name)
                .DistinctUntilChanged()
                .ToReactiveProperty(string.Empty);
        }

        public ListViewItemModel(
            DataStore                   dataStore,
            ListViewItemModelFactory    itemViewFactory,
            Data.Accounts.Type          type)
        {
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

            _name = ReactiveProperty.Create(type.ToString());
        }

        public ReactiveProperty<IReadOnlyList<ListViewItemModel>> Children
            => _children;

        public ReactiveProperty<string> Name
            => _name;

        private readonly ReactiveProperty<IReadOnlyList<ListViewItemModel>> _children;
        private readonly ReactiveProperty<string>                           _name;
    }
}
