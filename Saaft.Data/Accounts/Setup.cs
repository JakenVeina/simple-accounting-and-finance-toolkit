using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Data.Accounts
{
    public static class Setup
    {
        public static IServiceCollection AddAccounts(this IServiceCollection services)
            => services.AddTransient<Repository>();
    }
}
