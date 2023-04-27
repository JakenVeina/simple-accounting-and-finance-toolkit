using Saaft.Data;

namespace Saaft.Desktop.Workspaces
{
    public class ModelFactory
    {
        public ModelFactory(
            Data.Database.Repository    databaseRepository,
            DataStateStore              dataState,
            Database.ModelFactory       modelFactory)
        {
            _databaseRepository = databaseRepository;
            _dataState          = dataState;
            _modelFactory       = modelFactory;
        }

        public MainWorkspaceModel CreateMain()
            => new(
                databaseRepository: _databaseRepository,
                dataState:          _dataState,
                modelFactory:       _modelFactory);

        private readonly Data.Database.Repository   _databaseRepository;
        private readonly DataStateStore             _dataState;
        private readonly Database.ModelFactory      _modelFactory;
    }
}
