using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class ListViewItemModelFactory
    {
        public ListViewItemModelFactory(
            FormWorkspaceModelFactory   formWorkspaceFactory,
            Repository                  repository)
        {
            _formWorkspaceFactory   = formWorkspaceFactory;
            _repository             = repository;
        }

        public ListViewItemModel Create(long accountId)
            => new(
                formWorkspaceFactory:   _formWorkspaceFactory,
                itemFactory:            this,
                repository:             _repository,
                accountId:              accountId);

        public ListViewItemModel Create(Type type)
            => new(
                formWorkspaceFactory:   _formWorkspaceFactory,
                itemFactory:            this,
                repository:             _repository,
                type:                   type);

        private readonly FormWorkspaceModelFactory  _formWorkspaceFactory;
        private readonly Repository                 _repository;
    }
}
