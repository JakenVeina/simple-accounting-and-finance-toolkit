using Saaft.Data.Database;

namespace Saaft.Desktop.Database
{
    public class ModelFactory
    {
        public ModelFactory(
            Accounts.ModelFactory   modelFactory,
            FileStateStore          fileState,
            Repository              repository)
        {
            _fileState      = fileState;
            _modelFactory   = modelFactory;
            _repository     = repository;
        }

        public FileWorkspaceModel CreateFileWorkspace()
            => new(
                fileState:      _fileState,
                modelFactory:   this,
                repository:     _repository);

        public FileViewModel CreateFileView()
            => new(
                fileState:      _fileState,
                modelFactory:   _modelFactory);
        
        private readonly FileStateStore             _fileState;
        private readonly Accounts.ModelFactory      _modelFactory;
        private readonly Repository                 _repository;

    }
}
