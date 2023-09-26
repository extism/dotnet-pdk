# .NET PDK

This repo houses the .NET PDK for building Extism plugins in C# and F#.

> NOTE: This is an experimental PDK. We'd love to hear your feedback.

Join the [Discord](https://discord.gg/5g3mtQRt) and chat with us!

## Prerequisites
1. .NET SDK 8: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. WASI Workload:
```
dotnet workload install wasi-experimental
```
3. WASI SDK: https://github.com/WebAssembly/wasi-sdk/releases

## Installation
Create a new project and add this nuget package to your project:

```
dotnet new wasiconsole -o MyPlugin
cd MyPlugin
dotnet add package Extism.Pdk 
```

Update your MyPlugin.csproj as follows:
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

This will create a `MyPlugin.wasm` file in `bin/Debug/net8.0/wasi-wasm/AppBundle`. Now, you can try out your plugin by using any of [Extism SDKs](https://extism.org/docs/category/integrate-into-your-codebase).

## Example Usage
### Using Config, I/O, & Persisted Variables

```csharp
using System.Text;
using Extism;

// Read input from the host
var input = Pdk.GetInputString();

var count = 0;

foreach (var c in input)
{
    if ("aeiouAEIOU".Contains(c))
    {
        count++;
    }
}

// Read configuration values from the host
if (!Pdk.TryGetConfig("thing", out var thing))
{
    thing = "<unset by host>";
}

// Read variables persisted by the host
if (!Pdk.TryGetVar("total", out var totalBlock))
{
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
open Extism

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

### Making HTTP calls
WASI doesn't allow guests to create socket connections yet, and thus they can't make HTTP calls. However, Extism provides convenient functions to make HTTP calls easy. If the host is configured to allow them, Extism plugins can make http calls by using `Pdk.SendRequest`:

```csharp
var request = new HttpRequest("https://jsonplaceholder.typicode.com/todos/1")
{
    Method = HttpMethod.GET
};

request.Headers.Add("some-header", "value");

var response = Pdk.SendRequest(request);

Pdk.SetOutput(response.Body);
```

```fsharp
open Extism

let request = Extism.HttpRequest("https://jsonplaceholder.typicode.com/todos/1")
request.Method = HttpMethod.GET
request.Headers.Add("some-header", "value")

let response = Pdk.SendRequest(request)
Pdk.SetOutput(response.Body)
```

Output:
```json
{
  "userId": 1,
  "id": 1,
  "title": "delectus aut autem",
  "completed": false
}
```
### Export functions

If you want to export multiple functions from one plugin, you can use `UnmanagedCallersOnly` attribute:

```csharp
[UnmanagedCallersOnly(EntryPoint = "count_vowels")]
public static unsafe int CountVowels()
{
   var text = Pdk.GetInputString();

   // ...

   Pdk.SetOutput(result);
   return 0;
}
```

```fsharp
[<UnmanagedCallersOnly(EntryPoint = "count_vowels")>]
let CountVowels () =
  let buffer = Pdk.GetInput ()
  
  // ...

  Pdk.SetOutput ($"""{{ "count": {count} }}""")
  0
```

Notes:
1. If `UnmanagedCallersOnly.EntryPoint` is not specified, the method name will be used.
2. Exported functions can only have this signature: () => int.

### Import functions

The host might give guests additional capabilities. You can import functions from the host using `DllImport`:

```csharp
[DllImport("host", EntryPoint = "is_vowel")]
public static extern int IsVowel(int c);
```

```fsharp
[<DllImport("host", EntryPoint = "is_vowel")>]
extern int IsVowel(int c)
```

Notes:
1. Parameters and return types can only be one of these types: `SByte`, `Int16`, `Int32`, `Int64`, `Byte`, `UInt16`, `UInt32`, `UInt64`, `Float`, `Double`, and `Void`.
2. If `DllImport.EntryPoint` is not specified, the name of the method will be used.

## Samples
For more examples, check out the [samples](./samples) folder.