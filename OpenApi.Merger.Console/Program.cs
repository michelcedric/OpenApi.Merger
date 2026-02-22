using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenApi.Merger;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

builder.Services.AddLogging(logging =>
{
	logging.ClearProviders();
	logging.AddConsole();
});

builder.Services.AddOpenApiMerger(builder.Configuration);

var host = builder.Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

var merger = services.GetRequiredService<OpenApiMerger>();
var json = await merger.MergeMultipleApisAsJsonAsync();

await File.WriteAllTextAsync("output.json", json);

Console.ReadLine();