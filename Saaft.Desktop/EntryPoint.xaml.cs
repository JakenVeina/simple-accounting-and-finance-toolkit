using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using Saaft.Common;
using Saaft.Data;
using Saaft.Desktop.Database;

namespace Saaft.Desktop
{
    public partial class EntryPoint
        : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            _serviceProvider = new ServiceCollection()
                .AddSaaftCommon()
                .AddSaaftData()
                .AddSaaftDesktop()
                .BuildServiceProvider(new ServiceProviderOptions()
                {
                    ValidateOnBuild = true,
                    ValidateScopes  = true
                });

            _fileWorkspace = _serviceProvider.GetRequiredService<ModelFactory>()
                .CreateFileWorkspace();

            _hostWindow = new HostWindow()
            {
                DataContext = _fileWorkspace
            };
            _hostWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_hostWindow is not null)
            {
                _hostWindow.Close();
                _hostWindow = null;
            }

            if (_fileWorkspace is not null)
            {
                _fileWorkspace.Dispose();
                _fileWorkspace = null;
            }

            if (_serviceProvider is not null)
            {
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }

            base.OnExit(e);
        }

        private FileWorkspaceModel? _fileWorkspace;
        private HostWindow?         _hostWindow;
        private ServiceProvider?    _serviceProvider;
    }
}
