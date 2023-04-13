using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class ListViewItemModelFactory
    {
        public ListViewItemModelFactory(
            DataStore                   dataStore,
            FormWorkspaceModelFactory   formWorkspaceFactory)
        {
            _dataStore              = dataStore;
            _formWorkspaceFactory   = formWorkspaceFactory;
        }

        public ListViewItemModel Create(long accountId)
            => new(
                dataStore:              _dataStore,
                itemFactory:            this,
                formWorkspaceFactory:   _formWorkspaceFactory,
                accountId:              accountId);

        public ListViewItemModel Create(Type type)
            => new(
                dataStore:              _dataStore,
                itemViewFactory:        this,
                formWorkspaceFactory:   _formWorkspaceFactory,
                type:                   type);

        private readonly DataStore                  _dataStore;
        private readonly FormWorkspaceModelFactory  _formWorkspaceFactory;
    }
}
