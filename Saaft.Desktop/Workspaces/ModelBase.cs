using System.ComponentModel;

namespace Saaft.Desktop.Workspaces
{
    public abstract class ModelBase
    {
        public abstract ReactiveProperty<string> Title { get; }
    }
}
