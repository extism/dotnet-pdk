using CliWrap;
using CliWrap.Buffered;
using Shouldly;

namespace Extism.Pdk.WasmTests;

public class KitchenSinkTests
{
    [Fact]
    public async void TestLen()
    {
        var path = GetWasmPath("KitchenSink");
        var (stdout, exit, stderr) = await Call(path, "len", new ExtismOptions
        {
            Loop = 3,
            Input = "Hello World!"
        });

        stderr.ShouldBe("");
        exit.ShouldBe(0);
        stdout.ShouldBe("12\n12\n12\n");
    }

    [Fact]
    public async void TestConcat()
    {
        var path = GetWasmPath("KitchenSink");
        var (stdout, exit, stderr) = await Call(path, "concat", new ExtismOptions
        {
            Loop = 3,
            Input = $$"""{ "Separator": ",", "Parts": ["hello", "world!"]}"""
        });

        stderr.ShouldBe("");
        exit.ShouldBe(0);
        stdout.ShouldBe("hello,world!\nhello,world!\nhello,world!\n");
    }

    [Fact]
    public async void TestCount()
    {
        var path = GetWasmPath("KitchenSink");
        var (stdout, exit, stderr) = await Call(path, "counter", new ExtismOptions
        {
            Loop = 3,
            Input = ""
        });

        stderr.ShouldBe("");
        exit.ShouldBe(0);
        // TODO: Enable this again after the Extism CLI is fixed
        //stdout.ShouldBe("1\n2\n3\n");
    }

    [Theory]
    [InlineData("", "Greetings, Anonymous!")]
    [InlineData("John", "Greetings, John!")]
    public async void TestConfig(string name, string expected)
    {
        var path = GetWasmPath("KitchenSink");
        var (stdout, exit, stderr) = await Call(path, "greeter", new ExtismOptions
        {
            Loop = 3,
            Input = "",
            Config = new Dictionary<string, string>
            {
                { "name", name }
            }
        });

        stderr.ShouldBe("");
        exit.ShouldBe(0);
        stdout.ShouldBe($"{expected}\n{expected}\n{expected}\n");
    }

    [Theory]
    [InlineData("123", "jsonplaceholder.*.com", true)]
    [InlineData("123", "", false)]
    [InlineData("", "jsonplaceholder.typicode.com", false)]
    public async void TestHttp(string token, string allowedHost, bool expected)
    {
        var path = GetWasmPath("KitchenSink");
        var (stdout, exit, stderr) = await Call(path, "get_todo", new ExtismOptions
        {
            Loop = 3,
            Input = "1",
            Config = new Dictionary<string, string>
            {
                { "api-token", token }
            },
            AllowedHosts = [allowedHost]
        });

        if (expected)
        {
            exit.ShouldBe(0);
        }
        else
        {
            exit.ShouldNotBe(0);
        }
    }

    [Fact]
    public async void TestThrow()
    {
        var path = GetWasmPath("KitchenSink");
        var (_, exit, stderr) = await Call(path, "throw", new ExtismOptions
        {
            Loop = 3,
            Input = "Hello World!"
        });

        stderr.ShouldContain("Something bad happened.");
        exit.ShouldBe(1);
    }

    // TODO: test function imports

    private string GetWasmPath(string name)
    {
        return Path.Combine(
            Environment.CurrentDirectory,
            $"../../../../../samples/{name}/bin/Debug/net8.0/wasi-wasm/AppBundle/{name}.wasm");
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
        public bool Quiet { get; set; }
        public Dictionary<string, string> Config { get; set; } = new();
        public List<string> AllowedHosts { get; set; } = new List<string>();

        public string[] ToCliArguments()
        {
            var wasiArg = Wasi ? "--wasi" : "";
            var quietArg = Quiet ? "-q" : "";

            var configs = Config.Where(c => !string.IsNullOrEmpty(c.Value)).SelectMany(c => new string[] { "--config", $"{c.Key}={c.Value}" });
            var hosts = AllowedHosts.Where(h => !string.IsNullOrEmpty(h)).SelectMany(h => new string[] { "--allow-host", h });

            return ["--loop", Loop.ToString(), "--input", Input, quietArg, wasiArg, .. configs, .. hosts];
        }
    }
}