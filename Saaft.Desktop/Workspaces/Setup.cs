using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Desktop.Workspaces
{
    internal static class Setup
    {
        public static IServiceCollection AddWorkspaces(this IServiceCollection services)
            => services.AddSingleton<ModelFactory>();
    }
}
