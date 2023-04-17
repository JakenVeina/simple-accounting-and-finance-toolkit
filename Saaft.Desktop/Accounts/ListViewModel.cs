using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

namespace Saaft.Desktop.Accounts
{
    public class ListViewModel
    {
        public ListViewModel(ModelFactory modelFactory)
            => _rootItems = Enum.GetValues<Data.Accounts.Type>()
                .OrderBy(type => type)
                .Select(type => Observable.Using(
                    () => modelFactory.CreateListViewItem(type),
                    item => Observable.Never<ListViewItemModel>().Prepend(item)))
                .CombineLatest(items => items.ToArray().AsReadOnly())
                .ToReactiveProperty(Array.Empty<ListViewItemModel>());

        public ReactiveProperty<IReadOnlyList<ListViewItemModel>> RootItems
            =>_rootItems;

        private readonly ReactiveProperty<IReadOnlyList<ListViewItemModel>> _rootItems;
    }
}
