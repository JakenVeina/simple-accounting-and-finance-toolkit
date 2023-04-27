using Saaft.Data;

namespace Saaft.Desktop.Database
{
    public class ModelFactory
    {
        public ModelFactory(
            DataStateStore          dataState,
            Accounts.ModelFactory   modelFactory)
        {
            _dataState      = dataState;
            _modelFactory   = modelFactory;
        }

        public FileViewModel CreateFileView()
            => new(
                dataState:      _dataState,
                modelFactory:   _modelFactory);

        private readonly DataStateStore         _dataState;
        private readonly Accounts.ModelFactory  _modelFactory;
    }
}
