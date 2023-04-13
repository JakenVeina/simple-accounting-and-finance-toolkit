using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Saaft.Data;

namespace Saaft.Desktop
{
    public partial class EntryPoint
        : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton<DataStore>()
                .AddTransient<Accounts.FormWorkspaceModelFactory>()
                .AddTransient<Accounts.ListViewItemModelFactory>()
                .AddTransient<Accounts.ListViewModel>()
                .AddTransient<Database.FileViewModel>()
                .AddTransient<Workspaces.MainModel>()
                .BuildServiceProvider();

            _hostWindow = new Workspaces.Window()
            {
                DataContext = _serviceProvider.GetRequiredService<Workspaces.MainModel>()
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

            if (_serviceProvider is not null)
            {
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }

            base.OnExit(e);
        }

        private ServiceProvider?    _serviceProvider;
        private Workspaces.Window?  _hostWindow;
    }
}
