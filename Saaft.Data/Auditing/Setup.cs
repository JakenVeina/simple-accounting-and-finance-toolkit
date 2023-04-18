using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Data.Auditing
{
    public static class Setup
    {
        public static IServiceCollection AddAuditing(this IServiceCollection services)
            => services.AddSingleton<Repository>();
    }
}
