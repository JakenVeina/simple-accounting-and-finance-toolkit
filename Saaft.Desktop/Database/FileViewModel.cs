using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;

using Saaft.Data;

namespace Saaft.Desktop.Database
{
    public class FileViewModel
    {
        public FileViewModel(DataStore dataStore)
            => _name = dataStore
                .WhereNotNull()
                .Select(file => file?.FilePath ?? "Untitled.saaft")
                .DistinctUntilChanged()
                .ToReactiveProperty();

        public ReactiveProperty<string?> Name
            => _name;

        private readonly ReactiveProperty<string?> _name;
    }
}
