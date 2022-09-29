using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ValidationExtensions
    {
        public static T GetValidatedConfig<T>(this IServiceCollection services, IConfigurationSection section) where T : class
        {
            services
                .AddOptions<T>()
                .Bind(section)
                .ValidateDataAnnotations();

            return services.BuildServiceProvider().GetRequiredService<IOptions<T>>().Value;
        }
    }
}