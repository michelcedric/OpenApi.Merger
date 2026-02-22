# OpenApi.Merger
[![CI / Publish NuGet](https://github.com/michelcedric/OpenApi.Merger/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/michelcedric/OpenApi.Merger/actions/workflows/publish-nuget.yml)
![NuGet Downloads](https://img.shields.io/nuget/dt/Extend.OpenApi.Source.Merger?style=flat)


Merge multiple OpenAPI (Swagger) JSON documents into a single specification. The solution contains a reusable library and a console host that reads configuration, fetches the swagger docs, merges them, and writes `output.json`.

## Projects
- Library: `OpenApi.Merger` (core merger logic, DI extension).
- Console: `OpenApi.Merger.Console` (example runner using the library and configuration).

## Configuration
The merger uses the `OpenApiMerger` section from `appsettings.json`:

```json
{
	"OpenApiMerger": {
		"OpenApiTitle": "Merged API",
		"OpenApiVersion": "v1",
		"OpenApiDescription": "Combined APIs",
		"Apis": [
			{
				"Name": "Api1",
				"ServerUrl": "http://localhost:5001",
				"PathPrefix": "",
				"OpenApiPath": "/swagger/v1/swagger.json"
			},
			{
				"Name": "Api2",
				"ServerUrl": "http://localhost:5002",
				"PathPrefix": "",
				"OpenApiPath": "/swagger/v1/swagger.json"
			}
		]
	}
}
```

- `OpenApiTitle`, `OpenApiVersion`, `OpenApiDescription`: metadata for the merged document.
- `Apis`: array of API entries to merge.
	- `Name`: friendly name used for tag/schema conflict prefixes.
	- `ServerUrl`: base URL for the API.
	- `PathPrefix`: prefix added before paths (use empty string if none).
	- `OpenApiPath`: relative path to the swagger JSON.
	- `FilePath` (optional): when present the merger will read the OpenAPI JSON from this local file instead of fetching the URL. Paths are resolved relative to the process working directory or may be absolute.

## Running the console merger
1) Ensure each target API serves swagger JSON at the configured URLs.
2) Edit `OpenApi.Merger.Console/appsettings.json` to match your services.
3) From the repo root, run:

```bash
dotnet run --project OpenApi.Merger.Console/OpenApi.Merger.Console.csproj
```

The console fetches all configured specs, merges them, and writes `output.json` in the working directory. It also logs conflicts (e.g., schema name collisions) to the console.

File support
 - You can point an API entry at a local OpenAPI JSON file by setting `FilePath` in the configuration. This is useful for running the merger in environments where backends are not available or for CI/tests. Example (appsettings.test.json):

```json
{
	"OpenApiMerger": {
		"OpenApiTitle": "Merged API",
		"OpenApiVersion": "v1",
		"Apis": [
			{ "Name": "Api1", "ServerUrl": "http://localhost:5001", "FilePath": "Resources/api1.json" },
			{ "Name": "Api2", "ServerUrl": "http://localhost:5002", "FilePath": "Resources/api2.json" }
		]
	}
}
```

 - When `FilePath` is provided the merger loads the file and will not call the `ServerUrl`/`OpenApiPath`.

## Using the library in your app
1) Reference `OpenApi.Merger` from your project.
2) Register the merger with configuration and DI:

```csharp
builder.Services.AddOpenApiMerger(builder.Configuration);
```

3) Inject and call the merger:

```csharp
public class MergeRunner(OpenApiMerger merger)
{
		public Task<string> RunAsync() => merger.MergeMultipleApisAsJsonAsync();
}
```

`MergeMultipleApisAsJsonAsync` returns the merged OpenAPI JSON string. `MergeMultipleApisAsync` returns the `OpenApiDocument` if you need to post-process before serialization.

### Expose merged JSON via one-line endpoint

You can expose the merged OpenAPI JSON from an ASP.NET Core web project with a single line in `Program.cs` (ensure you registered the merger with `builder.Services.AddOpenApiMerger(builder.Configuration)` first):

```csharp
app.MapGet("/openapi/merged.json", async (OpenApiMerger merger, HttpResponse res) => { res.ContentType = "application/json"; await res.WriteAsync(await merger.MergeMultipleApisAsJsonAsync()); });
```

This maps `/openapi/merged.json` to return the merged OpenAPI document as JSON at runtime.

## Notes
- Input format: JSON swagger documents.
- The merger prefixes conflicting schema names with the API `Name` and adds tag prefixes `<Name>-<Tag>`.
- Servers are normalized per API using `ServerUrl`; ensure the URLs are externally reachable from where you run the merger.