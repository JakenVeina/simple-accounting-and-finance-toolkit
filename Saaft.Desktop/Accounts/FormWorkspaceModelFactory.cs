using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class FormWorkspaceModelFactory
    {
        public FormWorkspaceModelFactory(
            DataStore   dataStore,
            Repository  repository)
        {
            _dataStore  = dataStore;
            _repository = repository;
        }

        public FormWorkspaceModel Create(CreationModel model)
            => new(
                dataStore:  _dataStore,
                repository: _repository,
                model:      model);

        private readonly DataStore  _dataStore;
        private readonly Repository _repository;
    }
}
