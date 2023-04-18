using System;

using Microsoft.Extensions.DependencyInjection;

namespace Saaft.Common
{
    public static class Setup
    {
        public static IServiceCollection AddSaaftCommon(this IServiceCollection services)
            => services.AddSingleton<SystemClock>();
    }
}
