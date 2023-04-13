using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class FormWorkspaceModelFactory
    {
        public FormWorkspaceModelFactory(DataStore dataStore)
            => _dataStore = dataStore;

        public FormWorkspaceModel Create(CreationModel model)
            => new(
                _dataStore,
                model);

        private readonly DataStore _dataStore;
    }
}
