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
                .OrderBy(static type => type)
                .Select(type => ReactiveDisposable
                    .Create(() => modelFactory.CreateListViewItem(type))
                    .ToReactiveReadOnlyProperty())
                .ToList();

        public IReadOnlyList<ReactiveReadOnlyProperty<ListViewItemModel?>> RootItems
            =>_rootItems;

        private readonly IReadOnlyList<ReactiveReadOnlyProperty<ListViewItemModel?>> _rootItems;
    }
}
