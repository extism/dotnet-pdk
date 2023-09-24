# .NET PDK

This repo houses the .NET PDK for building Extism plugins in C# and F#.

## Prerequisites
1. .NET SDK 8: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. WASI Workload
```
dotnet workload install wasi-experimental
```

## Installation
You first need to install .NET 8:
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Then install the experimental WASI workload:

```
dotnet workload install wasi-experimental
```

Create a new project and add this nuget package to your project:

```
dotnet new wasiconsole -o MyPlugin
cd MyPlugin
dotnet add package Extism.Pdk 
```

Modify your MyPlugin.csproj like so:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>wasi-wasm</RuntimeIdentifier>
    <OutputType>Exe</OutputType>
    <PublishTrimmed>true</PublishTrimmed>

    <!-- Make sure we create a standalone wasm file for our plugin -->
    <WasmSingleFileBundle>true</WasmSingleFileBundle>
	<WasmBuildNative>true</WasmBuildNative>
  </PropertyGroup>
</Project>
```

Then compile your plugin to wasm:
```
dotnet build
```

This will create a `MyPlugin.wasm` file in `bin/Debug/net8.0/wasi-wasm/AppBundle`. Now, you can try out your plugin by using any of our SDKs, or by using [Wasmtime](https://wasmtime.dev/):
```
wasmtime run ./bin/debug/net8.0/wasi-wasm/AppBundle/MyPlugin.wasm                                       
Hello, Wasi Console!
```

## Example Usage
### Using Config, I/O, & Persisted Variables

```csharp
using System.Text;
using Extism.Pdk;

// Read input from the host
var input = Pdk.GetInputString();

var count = 0;

foreach (var c in input) {
    if ("aeiouAEIOU".Contains(c)) {
        count++;
    }
}

// Read configuration values from the host
if (!Pdk.TryGetConfig("thing", out var thing)) {
    thing = "<unset by host>";
}

// Read variables persisted by the host
if (!Pdk.TryGetVar("total", out var totalBlock)) {
    Pdk.Log(LogLevel.Info, "First time running, total is not set.");
}

int.TryParse(Encoding.UTF8.GetString(totalBlock.ReadBytes()), out var total);

// Save total for next invocations
total += count;
totalBlock = Pdk.Allocate(total.ToString());
Pdk.SetVar("total", totalBlock);

// Set plugin output for host to read
var output = $$"""{"count": {{count}}, "config": "{{thing}}", "total": "{{total}}" }""";
Pdk.SetOutput(output);
```

If you build this app and use Extism's .NET SDK to run it:
```csharp
var output = plugin.CallFunction("_start", Encoding.UTF8.GetBytes("Hello World!"));
Console.WriteLine(Encoding.UTF8.GetString(output));

output = plugin.CallFunction("_start", Encoding.UTF8.GetBytes("Hello World!"));
Console.WriteLine(Encoding.UTF8.GetString(output));
```

You'll get this output:
```
{"count": 3, "config": "<unset by host>", "total": "3" }
{"count": 3, "config": "<unset by host>", "total": "6" }
```

Notice how total is 6 in the second output.

The same example works in F# too!:
```fsharp
open System.Text
open Extism.Pdk

let countVowels (input: string) =
    input
    |> Seq.filter (fun c -> "aeiouAEIOU".Contains(c))
    |> Seq.length

// Read configuration from the host
let readConfig () =
    match Pdk.TryGetConfig("thing") with
    | true, thing -> thing
    | false, _ -> "<unset by the host>"

// Read a variable persisted by the host
let readTotal () =
    match Pdk.TryGetVar("total") with
    | true, totalBlock ->
        Encoding.UTF8.GetString(totalBlock.ReadBytes()) |> int
    | false, _ ->
        Pdk.Log(LogLevel.Info, "First time running, total is not set.")
        0

// Write a variable persisted by the host
let saveTotal total =
    let totalBlock = Pdk.Allocate(total.ToString())
    Pdk.SetVar("total", totalBlock)

[<EntryPoint>]
let main args =
    let input = Pdk.GetInputString()
    let count = countVowels input
    let thing = readConfig()
    let total = readTotal() + count
    saveTotal total

    let output = sprintf """{"count": %d, "config": "%s", "total": "%d" }""" count thing total
    Pdk.SetOutput(output)
    0
```