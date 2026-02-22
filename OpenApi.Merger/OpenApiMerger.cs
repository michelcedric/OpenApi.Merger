using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using System.Text;

namespace OpenApi.Merger
{
    public sealed class OpenApiMerger(IHttpClientFactory httpClientFactory, ILogger<OpenApiMerger> logger, IOptions<OpenApiMergerOptions> options)
    {
        private readonly OpenApiMergerOptions _options = options.Value;

        public async Task<string> MergeMultipleApisAsJsonAsync()
        {
            var document = await MergeMultipleApisAsync();

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                var jsonWriter = new OpenApiJsonWriter(writer);
                document.SerializeAsV3(jsonWriter);
            }

            return sb.ToString();
        }

        public async Task<OpenApiDocument> MergeMultipleApisAsync()
        {
            var loadedApis = new List<(ApiConfiguration Config, OpenApiDocument Document)>();

            foreach (var apiConfig in _options.Apis)
            {
                OpenApiDocument document;

                if (!string.IsNullOrWhiteSpace(apiConfig.FilePath))
                {
                    var fullPath = Path.GetFullPath(apiConfig.FilePath);
                    logger.LogInformation("Loading {Name} from file {Path}", apiConfig.Name, fullPath);
                    document = await LoadOpenApiDocumentFromFileAsync(apiConfig, fullPath);
                }
                else
                {
                    var fullUrl = $"{apiConfig.ServerUrl}{apiConfig.PathPrefix}{apiConfig.OpenApiPath}";
                    logger.LogInformation("Loading {Name} from {Url}", apiConfig.Name, fullUrl);
                    document = await LoadOpenApiDocumentFromUrlAsync(apiConfig, fullUrl);
                }

                document.Servers =
                [
                    new OpenApiServer { Url = apiConfig.ServerUrl }
                ];

                loadedApis.Add((apiConfig, document));
                logger.LogInformation("Loaded {Name} with {Count} paths", apiConfig.Name, document.Paths.Count);
            }

            return MergeDocuments(loadedApis);
        }

        private async Task<OpenApiDocument> LoadOpenApiDocumentFromFileAsync(ApiConfiguration config, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"OpenAPI file not found at {filePath}", filePath);

            var content = await File.ReadAllBytesAsync(filePath);
            using var stream = new MemoryStream(content);

            var baseUri = new Uri(filePath, UriKind.Absolute);
            var settings = new OpenApiReaderSettings();

            var reader = new OpenApiJsonReader();
            var result = reader.Read(stream, baseUri, settings);

            return HandleReadResult(config, result);
        }

        private async Task<OpenApiDocument> LoadOpenApiDocumentFromUrlAsync(ApiConfiguration config, string url)
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            var baseUri = new Uri(url, UriKind.Absolute);
            var settings = new OpenApiReaderSettings();

            var reader = new OpenApiJsonReader();
            var result = reader.Read(stream, baseUri, settings);

            return HandleReadResult(config, result);
        }

        private OpenApiDocument HandleReadResult(ApiConfiguration config, ReadResult result)
        {
            var document = result.Document ?? throw new InvalidOperationException("Failed to parse OpenAPI document as JSON.");
            var diagnostic = result.Diagnostic ?? new OpenApiDiagnostic();

            if (diagnostic.Errors.Any())
            {
                logger.LogWarning(
                    "Warnings while loading {Name}: {Errors}",
                    config.Name,
                    string.Join(", ", diagnostic.Errors.Select(e => e.Message)));
            }

            return document;
        }

        private OpenApiDocument MergeDocuments(List<(ApiConfiguration Config, OpenApiDocument Document)> apis)
        {
            var merged = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Title = _options.OpenApiTitle,
                    Version = _options.OpenApiVersion,
                    Description = _options.OpenApiDescription
                },
                Paths = [],
                Components = new OpenApiComponents
                {
                    Schemas = new Dictionary<string, IOpenApiSchema>(),
                    SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(),
                    Parameters = new Dictionary<string, IOpenApiParameter>(),
                    RequestBodies = new Dictionary<string, IOpenApiRequestBody>(),
                    Responses = new Dictionary<string, IOpenApiResponse>(),
                    Headers = new Dictionary<string, IOpenApiHeader>(),
                    Examples = new Dictionary<string, IOpenApiExample>(),
                    Links = new Dictionary<string, IOpenApiLink>(),
                    Callbacks = new Dictionary<string, IOpenApiCallback>()
                },
                Tags = new HashSet<OpenApiTag>(),
                Servers = []
            };

            var schemaConflicts = new Dictionary<string, int>();
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
            var allServers = new List<OpenApiServer>();

            foreach (var (config, document) in apis)
            {
                logger.LogInformation("Merging {Name}...", config.Name);

                MergePaths(merged, document, config);
                MergeSchemas(merged, document, config.Name, schemaConflicts);

                if (document.Components?.SecuritySchemes != null)
                {
                    foreach (var scheme in document.Components.SecuritySchemes)
                        securitySchemes.TryAdd(scheme.Key, scheme.Value);
                }

                if (document.Servers != null)
                {
                    foreach (var server in document.Servers)
                    {
                        if (allServers.All(s => s.Url != server.Url))
                            allServers.Add(server);
                    }
                }

                MergeTags(merged, document, config.Name);
                MergeComponents(merged, document, config.Name);
            }

            merged.Servers = allServers;

            foreach (var scheme in securitySchemes)
                merged.Components.SecuritySchemes.TryAdd(scheme.Key, scheme.Value);

            return merged;
        }

        private void MergePaths(OpenApiDocument merged, OpenApiDocument source, ApiConfiguration config)
        {
            foreach (var path in source.Paths)
            {
                var pathKey = path.Key;

                var prefixedPath = pathKey.StartsWith(config.PathPrefix)
                    ? pathKey
                    : (pathKey.StartsWith("/")
                        ? $"{config.PathPrefix}{pathKey}"
                        : $"{config.PathPrefix}/{pathKey}");

                if (merged.Paths.ContainsKey(prefixedPath))
                {
                    logger.LogWarning("Path conflict: {Path} already exists, skipping", prefixedPath);
                    continue;
                }

                merged.Paths.Add(prefixedPath, path.Value);
            }
        }

        private void MergeSchemas(OpenApiDocument merged, OpenApiDocument source, string apiName, Dictionary<string, int> conflicts)
        {
            var components = merged.Components;
            var sourceComponents = source.Components;

            if (components == null || components.Schemas == null || sourceComponents?.Schemas == null) return;

            var targetSchemas = components.Schemas;
            var sourceSchemas = sourceComponents.Schemas;

            foreach (var schema in sourceSchemas)
            {
                var schemaKey = schema.Key;

                if (targetSchemas.ContainsKey(schemaKey))
                {
                    var originalKey = schemaKey;
                    schemaKey = $"{apiName}{schema.Key}";

                    conflicts.TryAdd(originalKey, 0);
                    conflicts[originalKey]++;

                    logger.LogWarning("Schema conflict: {Original} renamed to {New}", originalKey, schemaKey);
                }

                targetSchemas.Add(schemaKey, schema.Value);
            }
        }

        private void MergeTags(OpenApiDocument merged, OpenApiDocument source, string apiName)
        {
            if (source.Tags == null) return;

            var tags = merged.Tags ??= new HashSet<OpenApiTag>();

            foreach (var tag in source.Tags)
            {
                var tagName = $"{apiName}-{tag.Name}";

                if (tags.Any(t => t.Name == tagName)) continue;
                var item = new OpenApiTag
                {
                    Name = tagName,
                    Description = tag.Description,
                    ExternalDocs = tag.ExternalDocs
                };
                tags.Add(item);
            }
        }

        private void MergeComponents(OpenApiDocument merged, OpenApiDocument source, string apiName)
        {
            MergeComponentDictionary(merged.Components?.Parameters, source.Components?.Parameters, apiName);
            MergeComponentDictionary(merged.Components?.RequestBodies, source.Components?.RequestBodies, apiName);
            MergeComponentDictionary(merged.Components?.Responses, source.Components?.Responses, apiName);
            MergeComponentDictionary(merged.Components?.Headers, source.Components?.Headers, apiName);
            MergeComponentDictionary(merged.Components?.Examples, source.Components?.Examples, apiName);
            MergeComponentDictionary(merged.Components?.Links, source.Components?.Links, apiName);
            MergeComponentDictionary(merged.Components?.Callbacks, source.Components?.Callbacks, apiName);
        }

        private static void MergeComponentDictionary<T>(IDictionary<string, T>? target, IDictionary<string, T>? source, string apiName)
        {
            if (target == null || source == null) return;

            foreach (var item in source)
            {
                var key = target.ContainsKey(item.Key) ? $"{apiName}_{item.Key}" : item.Key;
                target.TryAdd(key, item.Value);
            }
        }
    }
}