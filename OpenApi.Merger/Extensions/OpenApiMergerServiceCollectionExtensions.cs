using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenApi.Merger;

public static class OpenApiMergerServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiMerger(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<OpenApiMergerOptions>(configuration.GetRequiredSection(OpenApiMergerOptions.SectionName));
        services.AddHttpClient();
        services.AddSingleton<OpenApiMerger>();

        return services;
    }
}
