using Saaft.Data.Database;

namespace Saaft.Desktop.Database
{
    public class ModelFactory
    {
        public ModelFactory(
            FileStateStore          fileState,
            Accounts.ModelFactory   modelFactory)
        {
            _fileState      = fileState;
            _modelFactory   = modelFactory;
        }

        public FileViewModel CreateFileView()
            => new(
                fileState:      _fileState,
                modelFactory:   _modelFactory);

        private readonly FileStateStore         _fileState;
        private readonly Accounts.ModelFactory  _modelFactory;
    }
}
