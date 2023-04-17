using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop.Workspaces
{
    public static class Setup
    {
        public static IServiceCollection AddWorkspaces(this IServiceCollection services)
            => services.AddTransient<ModelFactory>();
    }
}
