using System.Reflection;
using Microsoft.OpenApi;

Console.WriteLine("Methods on OpenApiDocument containing 'Serialize':");
foreach (var m in typeof(OpenApiDocument).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
{
	if (m.Name.Contains("Serialize"))
	{
		Console.WriteLine(m);
	}
}

Console.WriteLine();
Console.WriteLine("Types containing 'JsonWriter':");
var asm = typeof(OpenApiDocument).Assembly;
foreach (var t in asm.GetTypes().Where(t => t.Name.Contains("JsonWriter")))
{
	Console.WriteLine(t.FullName);
	foreach (var ctor in t.GetConstructors())
	{
		Console.WriteLine("  " + ctor);
	}
}

static void DumpMethods(Type t) { }
static void DumpProperties(Type t) { }
