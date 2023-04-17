using Saaft.Data;

namespace Saaft.Desktop.Workspaces
{
    public class ModelFactory
    {
        public ModelFactory(
            DataStore               dataStore,
            Database.ModelFactory   modelFactory)
        {
            _dataStore      = dataStore;
            _modelFactory   = modelFactory;
        }

        public MainModel CreateMain()
            => new(
                dataStore:      _dataStore,
                modelFactory:   _modelFactory);

        private readonly DataStore              _dataStore;
        private readonly Database.ModelFactory  _modelFactory;
    }
}
