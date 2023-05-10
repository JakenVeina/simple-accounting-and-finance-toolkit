using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop.Accounts
{
    internal static class Setup
    {
        public static IServiceCollection AddAccounts(this IServiceCollection services)
            => services.AddSingleton<ModelFactory>();
    }
}
