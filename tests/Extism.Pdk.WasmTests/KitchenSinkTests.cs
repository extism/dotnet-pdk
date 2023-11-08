using CliWrap;
using CliWrap.Buffered;
using Extism.Sdk.Native;
using Shouldly;
using Extism.Sdk;
using System.Text;

namespace Extism.Pdk.WasmTests;

public class KitchenSinkTests
{
    [Fact]
    public void TestLen()
    {
        using var plugin = CreatePlugin("KitchenSink");

        for (var i = 0; i < 3; i++)
        {
            var result = plugin.Call("len", Encoding.UTF8.GetBytes("Hello World!"));
            var stdout = Encoding.UTF8.GetString(result);
            stdout.ShouldBe("12");
        }
    }

    [Fact]
    public void TestConcat()
    {
        using var plugin = CreatePlugin("KitchenSink");

        var input = Encoding.UTF8.GetBytes("""{ "Separator": ",", "Parts": ["hello", "world!"]}""");

        for (var i = 0; i < 3; i++)
        {
            var result = plugin.Call("concat", input);
            var stdout = Encoding.UTF8.GetString(result);
            stdout.ShouldBe("hello,world!");
        }
    }

    [Fact]
    public void TestCount()
    {
        using var plugin = CreatePlugin("KitchenSink");

        for (var i = 1; i <= 3; i++)
        {
            var result = plugin.Call("counter", []);
            var stdout = Encoding.UTF8.GetString(result);
            stdout.ShouldBe(i.ToString());
        }
    }

    [Theory]
    [InlineData("", "Greetings, Anonymous!")]
    [InlineData("John", "Greetings, John!")]
    public void TestConfig(string name, string expected)
    {
        using var plugin = CreatePlugin("KitchenSink", manifest =>
        {
            manifest.Config["name"] = name;
        });

        for (var i = 0; i < 3; i++)
        {
            var result = plugin.Call("greeter", []);
            var stdout = Encoding.UTF8.GetString(result);
            stdout.ShouldBe(expected);
        }
    }

    [Theory]
    [InlineData("123", "jsonplaceholder.typicode.com", true)]
    [InlineData("123", "", false)]
    [InlineData("", "jsonplaceholder.typicode.com", false)]
    public void TestHttp(string token, string allowedHost, bool expected)
    {
        using var plugin = CreatePlugin("KitchenSink", manifest =>
        {
            manifest.Config["api-token"] = token;

            manifest.AllowedHosts.Add(allowedHost);
        });

        var input = Encoding.UTF8.GetBytes("1");

        if (expected)
        {
            var result = plugin.Call("get_todo", input);
            var stdout = Encoding.UTF8.GetString(result);
            stdout.ShouldNotContain("error");
        }
        else
        {
            Should.Throw<ExtismException>(() => plugin.Call("get_todo", input));
        }
    }

    [Fact]
    public void TestThrow()
    {
        using var plugin = CreatePlugin("KitchenSink");

        for (var i = 0; i < 3; i++)
        {
            Should.Throw<ExtismException>(() => plugin.Call("throw", []))
                .Message.ShouldContain("Something bad happened.");
        }
    }

    [Fact]
    public void TestReferencedExport()
    {
        using var plugin = CreatePlugin("KitchenSink");

        for (var i = 0; i < 3; i++)
        {
            var result = plugin.Call("samplelib_export", []);
        }
    }

    private string GetWasmPath(string name)
    {
        return Path.Combine(
            Environment.CurrentDirectory,
            $"../../../../../samples/{name}/bin/Debug/net8.0/wasi-wasm/AppBundle/{name}.wasm");
    }

    private Plugin CreatePlugin(
        string name,
        Action<Manifest>? config = null,
        HostFunction[]? hostFunctions = null)
    {
        var source = new PathWasmSource(GetWasmPath(name));
        var manifest = new Manifest(source);

        if (config is not null)
        {
            config(manifest);
        }

        HostFunction[] functions = [
            HostFunction.FromMethod("samplelib_import", 0, (cp) => { }),
            .. (hostFunctions ?? [])
        ];

        foreach (var function in functions)
        {
            function.SetNamespace("env");
        }

        return new Plugin(manifest, functions, withWasi: true);
    }

    private async Task<(string, int, string)> Call(string wasmPath, string functionName, ExtismOptions options)
    {
        var args = new[] { "call", wasmPath, functionName }.Concat(options.ToCliArguments());

        var result = await Cli.Wrap("extism")
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        return (result.StandardOutput, result.ExitCode, result.StandardError);
    }

    class ExtismOptions
    {
        public int Loop { get; set; }
        public string Input { get; set; } = "";
        public bool Wasi { get; set; } = true;
        public Dictionary<string, string> Config { get; set; } = new();
        public List<string> AllowedHosts { get; set; } = new List<string>();

        public string[] ToCliArguments()
        {
            var wasiArg = Wasi ? "--wasi" : "";

            var configs = Config.Where(c => !string.IsNullOrEmpty(c.Value)).SelectMany(c => new string[] { "--config", $"{c.Key}={c.Value}" });
            var hosts = AllowedHosts.Where(h => !string.IsNullOrEmpty(h)).SelectMany(h => new string[] { "--allow-host", h });

            return ["--loop", Loop.ToString(), "--input", Input, wasiArg, .. configs, .. hosts];
        }
    }
}