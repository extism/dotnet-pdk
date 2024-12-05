using Shouldly;
using Extism.Sdk;
using System.Text;
using System.Text.Json;

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

            var output = JsonSerializer.Deserialize<ConcatOutput>(stdout);
            output.ShouldNotBeNull();

            output.Result.ShouldBe("hello,world!");
        }
    }

    [Fact]
    public void TestCount()
    {
        using var plugin = CreatePlugin("KitchenSink");

        for (var i = 1; i <= 3; i++)
        {
            var stdout = plugin.Call("counter", "");
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
            var stdout = plugin.Call("greeter", "");
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

        plugin.AllowHttpResponseHeaders();

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
            Should.Throw<ExtismException>(() => plugin.Call("throw", ""))
                .Message.ShouldContain("Something bad happened.");
        }
    }

    [Fact]
    public void TestReferencedExport()
    {
        using var plugin = CreatePlugin("KitchenSink");

        for (var i = 0; i < 3; i++)
        {
            _ = plugin.Call("samplelib_export", "");
        }
    }

    private string GetWasmPath(string name)
    {
        return Path.Combine(
            Environment.CurrentDirectory,
            $"../../../../../samples/{name}/bin/Release/net8.0/wasi-wasm/AppBundle/{name}.wasm");
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

    public class ConcatOutput
    {
        public string Result { get; set; } = default!;
    }
}