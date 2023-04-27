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
            DataStateStore          dataState,
            Accounts.ModelFactory   modelFactory)
        {
            _accountsList = modelFactory.CreateListView();

            _name = dataState
                .Select(dataState => string.Concat(
                    (dataState.LoadedFile.FilePath is null)
                        ? FileEntity.DefaultFilename
                        : Path.GetFileName(dataState.LoadedFile.FilePath),
                    dataState.LoadedFile.HasChanges
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
