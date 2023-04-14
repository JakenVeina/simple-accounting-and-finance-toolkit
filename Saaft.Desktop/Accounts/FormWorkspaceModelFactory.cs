using Saaft.Data;
using Saaft.Data.Accounts;

namespace Saaft.Desktop.Accounts
{
    public class FormWorkspaceModelFactory
    {
        public FormWorkspaceModelFactory(Repository repository)
            => _repository = repository;

        public FormWorkspaceModel Create(CreationModel model)
            => new(
                repository: _repository,
                model:      model);

        public FormWorkspaceModel Create(MutationModel model)
            => new(
                repository: _repository,
                model:      model);

        private readonly Repository _repository;
    }
}
