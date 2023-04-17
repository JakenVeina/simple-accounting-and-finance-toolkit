using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

using Saaft.Data;

namespace Saaft.Desktop.Database
{
    public class FileViewModel
    {
        public FileViewModel(
            DataStore               dataStore,
            Accounts.ModelFactory   modelFactory)
        {
            _accountsList = modelFactory.CreateListView();

            _name = dataStore
                .WhereNotNull()
                .Select(file => file?.FilePath ?? "Untitled.saaft")
                .DistinctUntilChanged()
                .ToReactiveProperty();
        }

        public Accounts.ListViewModel AccountsList
            => _accountsList;

        public ReactiveProperty<string?> Name
            => _name;

        private readonly Accounts.ListViewModel     _accountsList;
        private readonly ReactiveProperty<string?>  _name;
    }
}
