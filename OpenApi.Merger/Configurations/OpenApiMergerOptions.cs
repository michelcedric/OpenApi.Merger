namespace OpenApi.Merger;

public sealed class OpenApiMergerOptions
{
    public const string SectionName = "OpenApiMerger";

    public required string OpenApiTitle { get; set; }
    public required string OpenApiVersion { get; set; }
    public string? OpenApiDescription { get; set; }

    public ApiConfiguration[] Apis { get; set; } = [];
}
