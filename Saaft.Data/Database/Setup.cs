using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Data.Database
{
    public static class Setup
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services)
            => services.AddSingleton<Repository>();
    }
}
