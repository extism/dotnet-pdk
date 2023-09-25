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
        public string Input { get; set; }
        public bool Wasi { get; set; } = true;

        public string[] ToCliArguments()
        {
            var wasiArg = Wasi ? "--wasi" : "";
            return ["--loop", Loop.ToString(), "--input", Input, wasiArg];
        }
    }
}