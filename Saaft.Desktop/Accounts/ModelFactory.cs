using Saaft.Data;

namespace Saaft.Desktop.Accounts
{
    public class ModelFactory
    {
        public ModelFactory(Data.Accounts.Repository repository)
            => _repository = repository;

        public FormWorkspaceModel CreateFormWorkspace(Data.Accounts.CreationModel model)
            => new(
                repository: _repository,
                model:      model);

        public FormWorkspaceModel CreateFormWorkspace(Data.Accounts.MutationModel model)
            => new(
                repository: _repository,
                model:      model);

        public ListViewModel CreateListView()
            => new(
                modelFactory: this);

        public ListViewItemModel CreateListViewItem(ulong accountId)
            => new(
                modelFactory:   this,
                repository:     _repository,
                accountId:      accountId);

        public ListViewItemModel CreateListViewItem(Data.Accounts.Type type)
            => new(
                modelFactory:   this,
                repository:     _repository,
                type:           type);

        private readonly Data.Accounts.Repository _repository;
    }
}
