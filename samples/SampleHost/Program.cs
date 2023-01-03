using Extism.Sdk.Native;

using System.Text;

var path = "../../../../SamplePlugin/bin/Debug/net7.0/SamplePlugin.wasm";

var bytes = File.ReadAllBytes(path);
var context = new Context();
var plugin = context.CreatePlugin(bytes, withWasi: true);

if (plugin.FunctionExists("_start"))
{
    plugin.CallFunction("_start", Span<byte>.Empty);
}

var output = plugin.CallFunction("count_vowels", Encoding.UTF8.GetBytes("Hello World!"));

Console.WriteLine(Encoding.UTF8.GetString(output));