using Extism.Sdk;
using Extism.Sdk.Native;

using System.Text;

// var path = "../SampleCSharpPlugin/bin/debug/net8.0/wasi-wasm/AppBundle/SampleCSharpPlugin.wasm";
var path = "../SampleFSharpPlugin/bin/debug/net8.0/wasi-wasm/AppBundle/SampleFSharpPlugin.wasm";

Console.WriteLine(path);
var bytes = File.ReadAllBytes(path);
var context = new Context();

var hf = new HostFunction("is_vowel", "host", new ExtismValType[] { ExtismValType.I32 }, new ExtismValType[] { ExtismValType.I32 }, 0, IsVowel);

void IsVowel(CurrentPlugin plugin, Span<ExtismVal> inputs, Span<ExtismVal> outputs, nint userData)
{
    var bytes = plugin.ReadBytes(inputs[0].v.i32);

    var text = Encoding.UTF8.GetString(bytes);

    switch (char.ToLowerInvariant(text[0]))
    {

        case 'a':
        case 'A':
        case 'e':
        case 'E':
        case 'i':
        case 'I':
        case 'o':
        case 'O':
        case 'u':
        case 'U':
            outputs[0].v.i32 = 1;
            return;
    }

    outputs[0].v.i32 = 0;
}

var plugin = context.CreatePlugin(bytes, new HostFunction[] { }, withWasi: true);

// if (plugin.FunctionExists("_initialize"))
// {
//     plugin.CallFunction("_initialize", Span<byte>.Empty);
// }

var output = plugin.CallFunction("_start", Encoding.UTF8.GetBytes("Hello World!"));
Console.WriteLine(Encoding.UTF8.GetString(output));

output = plugin.CallFunction("_start", Encoding.UTF8.GetBytes("Hello World!"));
Console.WriteLine(Encoding.UTF8.GetString(output));
