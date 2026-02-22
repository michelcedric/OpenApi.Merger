namespace OpenApi.Merger;

/// <summary>
/// Options for the OpenApi merger. Bound from the <c>OpenApiMerger</c> configuration section.
/// </summary>
public sealed class OpenApiMergerOptions
{
    /// <summary>The configuration section name expected in configuration sources.</summary>
    public const string SectionName = "OpenApiMerger";

    /// <summary>The title to use for the merged OpenAPI document.</summary>
    public required string OpenApiTitle { get; set; }

    /// <summary>The version to use for the merged OpenAPI document.</summary>
    public required string OpenApiVersion { get; set; }

    /// <summary>Optional description for the merged OpenAPI document.</summary>
    public string? OpenApiDescription { get; set; }

    /// <summary>Array of APIs to load and merge.</summary>
    public ApiConfiguration[] Apis { get; set; } = Array.Empty<ApiConfiguration>();
}
