using Microsoft.Extensions.DependencyInjection;

using Saaft.Data.Accounts;

namespace Saaft.Data
{
    public static class Setup
    {
        public static IServiceCollection AddSaaftData(this IServiceCollection services)
            => services
                .AddAccounts()
                .AddSingleton<DataStore>();
    }
}
