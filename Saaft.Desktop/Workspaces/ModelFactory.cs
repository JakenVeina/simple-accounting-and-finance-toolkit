using Saaft.Data.Database;

namespace Saaft.Desktop.Workspaces
{
    public class ModelFactory
    {
        public ModelFactory(
            Data.Database.Repository    databaseRepository,
            FileStateStore              dataState,
            Database.ModelFactory       modelFactory)
        {
            _databaseRepository = databaseRepository;
            _dataState          = dataState;
            _modelFactory       = modelFactory;
        }

        public MainWorkspaceModel CreateMain()
            => new(
                databaseRepository: _databaseRepository,
                fileState:          _dataState,
                modelFactory:       _modelFactory);

        private readonly Data.Database.Repository   _databaseRepository;
        private readonly FileStateStore             _dataState;
        private readonly Database.ModelFactory      _modelFactory;
    }
}
