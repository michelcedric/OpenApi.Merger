using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenApi.Merger.Tests;

[TestClass]
public class OpenApiMergerTests
{
    [TestMethod]
    public async Task Merge_From_Files_Produces_Merged_OpenApi()
    {
        var basePath = AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenApiMerger(configuration);

        await using var provider = services.BuildServiceProvider();

        var merger = provider.GetRequiredService<OpenApiMerger>();

        var mergedDocument = await merger.MergeMultipleApisAsync();

        // Validate the merged OpenAPI document
        var validationErrors = mergedDocument.Validate(ValidationRuleSet.GetDefaultRuleSet()).ToArray();
        Assert.AreEqual(0, validationErrors.Length, "Merged OpenAPI document should have no validation errors");
    }
}
