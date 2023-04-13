using System.Windows;

using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop
{
    public partial class EntryPoint
        : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            _serviceProvider = new ServiceCollection()
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
