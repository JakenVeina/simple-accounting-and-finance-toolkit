using Saaft.Data;

namespace Saaft.Desktop.Database
{
    public class ModelFactory
    {
        public ModelFactory(
            DataStore               dataStore,
            Accounts.ModelFactory   modelFactory)
        {
            _dataStore      = dataStore;
            _modelFactory   = modelFactory;
        }

        public FileViewModel CreateFileView()
            => new(
                dataStore:      _dataStore,
                modelFactory:   _modelFactory);

        private readonly DataStore              _dataStore;
        private readonly Accounts.ModelFactory  _modelFactory;
    }
}
