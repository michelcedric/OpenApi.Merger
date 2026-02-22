namespace OpenApi.Merger
{
    /// <summary>
    /// Configuration for a single API to include in the merge. This can point to a remote
    /// OpenAPI JSON document (via <see cref="ServerUrl"/> + <see cref="OpenApiPath"/>)
    /// or to a local file via <see cref="FilePath"/>.
    /// </summary>
    public sealed class ApiConfiguration
    {
        /// <summary>Friendly name for the API. Used to prefix conflicting schema and tag names.</summary>
        public required string Name { get; set; }

        /// <summary>Base URL of the API (used to build the spec URL and to populate the merged Servers list).</summary>
        public required string ServerUrl { get; set; }

        /// <summary>Path prefix to add before each path from the source API when merging.</summary>
        public required string PathPrefix { get; set; }

        /// <summary>Relative path to the OpenAPI JSON on the server (e.g. <c>/swagger/v1/swagger.json</c>).</summary>
        public required string OpenApiPath { get; set; }

        /// <summary>
        /// Optional local file path to an OpenAPI JSON file. When set, the merger will load the
        /// document from this file instead of fetching it from <see cref="ServerUrl"/> + <see cref="OpenApiPath"/>.
        /// </summary>
        public string? FilePath { get; set; }
    }
}
