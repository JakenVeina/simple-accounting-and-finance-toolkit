using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

using Saaft.Data;
using Saaft.Data.Database;

namespace Saaft.Desktop.Database
{
    public class FileViewModel
    {
        public FileViewModel(
            DataStore               dataStore,
            Accounts.ModelFactory   modelFactory)
        {
            _accountsList = modelFactory.CreateListView();

            _name = dataStore
                .WhereNotNull()
                .Select(file => string.Concat(
                    (file.FilePath is null)
                        ? FileEntity.DefaultFilename
                        : Path.GetFileName(file.FilePath),
                    file.HasChanges
                        ? "*"
                        : ""))
                .DistinctUntilChanged()
                .ToReactiveProperty();
        }

        public Accounts.ListViewModel AccountsList
            => _accountsList;

        public ReactiveProperty<string?> Name
            => _name;

        private readonly Accounts.ListViewModel     _accountsList;
        private readonly ReactiveProperty<string?>  _name;
    }
}
