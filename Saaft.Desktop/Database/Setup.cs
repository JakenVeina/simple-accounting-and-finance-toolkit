using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop.Database
{
    public static class Setup
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services)
            => services.AddSingleton<ModelFactory>();
    }
}
