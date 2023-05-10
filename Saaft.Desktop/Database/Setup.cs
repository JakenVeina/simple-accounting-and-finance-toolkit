using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop.Database
{
    internal static class Setup
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services)
            => services.AddSingleton<ModelFactory>();
    }
}
