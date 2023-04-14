using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Data
{
    public static class Setup
    {
        public static IServiceCollection AddSaaftData(this IServiceCollection services)
            => services.AddSingleton<DataStore>();
    }
}
