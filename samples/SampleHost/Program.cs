using Extism.Pdk;
using Extism.Sdk;

using System.Text;
namespace ConsoleApp;

partial class Program
{
    static void Main(string[] args)
    {
        var path = "../SampleCSharpPlugin/bin/debug/net8.0/wasi-wasm/AppBundle/SampleCSharpPlugin.wasm";
        //var path = "../SampleFSharpPlugin/bin/debug/net8.0/wasi-wasm/AppBundle/SampleFSharpPlugin.wasm";

        Console.WriteLine(path);
        var bytes = File.ReadAllBytes(path);

        var hf = HostFunction.FromMethod<int, int>("is_vowel", IntPtr.Zero, IsVowel);

        int IsVowel(CurrentPlugin plugin, int x)
        {
            var c = (char)x;

            switch (char.ToLowerInvariant(c))
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
                    return 1;
            }

            return 0;
        }

        var plugin = new Plugin(bytes, new HostFunction[] { hf }, withWasi: true);

        var output = plugin.Call("count_vowels", Encoding.UTF8.GetBytes("Hello World!"));
        Console.WriteLine(Encoding.UTF8.GetString(output));

        output = plugin.Call("count_vowels", Encoding.UTF8.GetBytes("Hello World!"));
        Console.WriteLine(Encoding.UTF8.GetString(output));

    }

    [WasmFunction("stuff")]
    internal static void Stuff(string input)
    {

    }
    
}