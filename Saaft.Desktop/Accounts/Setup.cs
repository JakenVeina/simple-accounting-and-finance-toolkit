using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop.Accounts
{
    public static class Setup
    {
        public static IServiceCollection AddAccounts(this IServiceCollection services)
            => services
                .AddTransient<FormWorkspaceModelFactory>()
                .AddTransient<ListViewItemModelFactory>()
                .AddTransient<ListViewModel>();
    }
}
