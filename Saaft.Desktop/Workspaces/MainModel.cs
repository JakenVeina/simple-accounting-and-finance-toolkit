using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

using Saaft.Data;
using Saaft.Data.Database;
using Saaft.Desktop.Database;

namespace Saaft.Desktop.Workspaces
{
    public sealed class MainModel
        : ModelBase
    {
        public MainModel(
            DataStore       dataStore,
            FileViewModel   file)
        {
            _file = dataStore
                .Select(dataFile => (dataFile is null)
                    ? null
                    : file)
                .ToReactiveProperty();

            _newFileCommand = ReactiveCommand.Create(() => dataStore.Value = FileEntity.New);

            _title = ReactiveProperty.CreateStatic("Simple Accounting and Finance Toolkit");
        }

        public ReactiveCommand<Unit> NewFileCommand
            => _newFileCommand;

        public ReactiveProperty<FileViewModel?> File
            => _file;

        public override ReactiveProperty<string> Title
            => _title;

        private readonly ReactiveProperty<FileViewModel?>   _file;
        private readonly ReactiveCommand<Unit>              _newFileCommand;
        private readonly ReactiveProperty<string>           _title;
    }
}
