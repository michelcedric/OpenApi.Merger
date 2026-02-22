namespace OpenApi.Merger
{
    public sealed class ApiConfiguration
    {
        public required string Name { get; set; }
        public required string ServerUrl { get; set; }
        public required string PathPrefix { get; set; }
        public required string OpenApiPath { get; set; }
        public string? FilePath { get; set; }
    }
}
