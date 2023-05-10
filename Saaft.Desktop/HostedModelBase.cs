using System;
using System.ComponentModel;

namespace Saaft.Desktop
{
    public abstract class HostedModelBase
        : DisposableBase
    {
        public abstract ReactiveReadOnlyProperty<string> Title { get; }

        protected override void OnDisposing(DisposalType type) { }
    }
}
