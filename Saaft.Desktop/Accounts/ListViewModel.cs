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
                .Select(type => ReactiveDisposable
                    .Create(() => modelFactory.CreateListViewItem(type))
                    .ToReactiveProperty())
                .ToList();

        public IReadOnlyList<ReactiveProperty<ListViewItemModel?>> RootItems
            =>_rootItems;

        private readonly IReadOnlyList<ReactiveProperty<ListViewItemModel?>> _rootItems;
    }
}
