using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenApi.Merger;

/// <summary>
/// Extension methods to register OpenApi.Merger services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class OpenApiMergerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="OpenApiMerger"/> and related services into the <see cref="IServiceCollection"/>, binding
    /// <see cref="OpenApiMergerOptions"/> from the provided <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">Configuration containing the <c>OpenApiMerger</c> section.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
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
