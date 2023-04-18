using Microsoft.Extensions.DependencyInjection;

using Saaft.Data.Accounts;
using Saaft.Data.Auditing;

namespace Saaft.Data
{
    public static class Setup
    {
        public static IServiceCollection AddSaaftData(this IServiceCollection services)
            => services
                .AddAccounts()
                .AddAuditing()
                .AddSingleton<DataStore>();
    }
}
