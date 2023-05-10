using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Data.Accounts
{
    internal static class Setup
    {
        public static IServiceCollection AddAccounts(this IServiceCollection services)
            => services.AddSingleton<Repository>();
    }
}
