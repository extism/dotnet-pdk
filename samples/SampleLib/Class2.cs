using Extism;

using System.Text.Json.Serialization;

namespace SampleLib
{
    public class Functions
    {
        [JsonInputOutput(typeof(JsonContext))]
        [WasmFunction("greet")]
        public static GreetResponse Greet(GreetRequest request)
        {
            // TODO: implement greet
            return new GreetResponse();
        }
    }

    [JsonSerializable(typeof(GreetRequest))]
    [JsonSerializable(typeof(GreetResponse))]
    partial class JsonContext : JsonSerializerContext
    {

    }

    public class GreetRequest
    {

    }

    public class GreetResponse
    {

    }
}
