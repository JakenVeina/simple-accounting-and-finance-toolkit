using System.ComponentModel;

namespace Saaft.Desktop.Workspaces
{
    public sealed class MainModel
        : ModelBase
    {
        public MainModel()
            => _title = ReactiveProperty.CreateStatic("Simple Accounting and Finance Toolkit");

        public override ReactiveProperty<string> Title
            => _title;

        private readonly ReactiveProperty<string> _title;
    }
}
