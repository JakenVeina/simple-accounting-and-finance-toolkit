using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Saaft.Desktop.Accounts
{
    public class ListViewModel
    {
        public ListViewModel(ListViewItemModelFactory itemFactory)
            => _accountTypes = Enum.GetValues<Data.Accounts.Type>()
                .OrderBy(type => type)
                .Select(itemFactory.Create)
                .ToList();

        public IReadOnlyList<ListViewItemModel> AccountTypes
            =>_accountTypes;

        private readonly IReadOnlyList<ListViewItemModel> _accountTypes;
    }
}
