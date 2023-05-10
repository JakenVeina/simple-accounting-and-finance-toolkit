using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Data.Database
{
    internal static class Setup
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services)
            => services
                .AddSingleton<FileStateStore>()
                .AddSingleton<Repository>();
    }
}
