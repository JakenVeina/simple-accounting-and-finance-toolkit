using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class ListViewItemModelFactory
    {
        public ListViewItemModelFactory(DataStore dataStore)
            => _dataStore = dataStore;

        public ListViewItemModel Create(long accountId)
            => new(
                dataStore:      _dataStore,
                itemFactory:    this,
                accountId:      accountId);

        public ListViewItemModel Create(Type type)
            => new(
                dataStore:       _dataStore,
                itemViewFactory: this,
                type:            type);

        private readonly DataStore _dataStore;
    }
}
