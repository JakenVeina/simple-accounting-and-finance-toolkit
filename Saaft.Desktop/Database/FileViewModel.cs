using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Saaft.Data.Database;

namespace Saaft.Desktop.Database
{
    public class FileViewModel
    {
        public FileViewModel(
            FileStateStore          fileState,
            Accounts.ModelFactory   modelFactory)
        {
            _accountsList = modelFactory.CreateListView();

            _name = fileState
                .Select(static fileState => string.Concat(
                    (fileState.LoadedFile.FilePath is null)
                        ? FileEntity.DefaultFilename
                        : Path.GetFileName(fileState.LoadedFile.FilePath),
                    fileState.LoadedFile.HasChanges
                        ? "*"
                        : ""))
                .DistinctUntilChanged()
                .ToReactiveReadOnlyProperty();
        }

        public Accounts.ListViewModel AccountsList
            => _accountsList;

        public ReactiveReadOnlyProperty<string?> Name
            => _name;

        private readonly Accounts.ListViewModel             _accountsList;
        private readonly ReactiveReadOnlyProperty<string?>  _name;
    }
}
