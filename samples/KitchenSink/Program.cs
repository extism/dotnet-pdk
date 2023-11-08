using System.Runtime.InteropServices;
using System;
using Extism;
using System.Text.Json;
using SampleLib;
using System.Text.Json.Serialization;

Class1.noop(); // Import Class1 from SampleLib so that it's included during compilation
Console.WriteLine("Hello world!");

namespace Functions
{
    public class Functions
    {
        [UnmanagedCallersOnly(EntryPoint = "len")]
        public static int Length()
        {
            var text = Pdk.GetInputString();

            Pdk.SetOutput(text.Length.ToString());
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "concat")]
        public static int Concat()
        {
            var json = Pdk.GetInput();
            var payload = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.ConcatInput);

            if (payload is null)
            {
                Pdk.Log(LogLevel.Error, "Failed to deserialize input.");
                return 3;
            }

            Pdk.SetOutput(string.Join(payload.Separator, payload.Parts));
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "counter")]
        public static void Counter()
        {
            int count = 0;
            if (Pdk.TryGetVar("counter", out var block))
            {
                var bytes = block.ReadBytes();
                count = BitConverter.ToInt32(bytes);
            }

            count += 1;
            Pdk.SetVar("counter", BitConverter.GetBytes(count));
            Pdk.SetOutput(count.ToString());
        }

        [UnmanagedCallersOnly(EntryPoint = "greeter")]
        public static void Greeter()
        {
            var name = "Anonymous";
            if (Pdk.TryGetConfig("name", out var value))
            {
                name = value;
            }

            Pdk.SetOutput($"Greetings, {name}!");
        }

        [UnmanagedCallersOnly(EntryPoint = "get_todo")]
        public static int GetTodo()
        {
            var id = int.Parse(Pdk.GetInputString());
            if (!Pdk.TryGetConfig("api-token", out var token))
            {
                Pdk.SetError("Expected 'api-token' in the configs.");
                return 1;
            }

            var request = new HttpRequest($"https://jsonplaceholder.typicode.com/todos/{id}");
            request.Headers.Add("Authorization", $"Basic {token}");

            var response = Pdk.SendRequest(request);
            Pdk.SetOutput(response.Body);
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "throw")]
        public static int Throw()
        {
            // Exceptions are also handled, but Pdk.SetError is recommeded to use.
            throw new InvalidOperationException("Something bad happened.");
        }
    }

    [JsonSerializable(typeof(ConcatInput))]
    public partial class SourceGenerationContext : JsonSerializerContext {}

    public class ConcatInput
    {
        public string[] Parts { get; set; }
        public string? Separator { get; set; }
    }
}
