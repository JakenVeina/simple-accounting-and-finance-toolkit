using Microsoft.Extensions.DependencyInjection;

using Saaft.Desktop.Accounts;
using Saaft.Desktop.Database;
using Saaft.Desktop.Workspaces;

namespace Saaft.Desktop
{
    public static class Setup
    {
        public static IServiceCollection AddSaaftDesktop(this IServiceCollection services)
            => services
                .AddAccounts()
                .AddDatabase()
                .AddWorkspaces();
    }
}
