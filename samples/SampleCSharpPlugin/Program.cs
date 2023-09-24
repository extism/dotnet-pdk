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

// Make an HTTP request
// var request = new Extism.Pdk.HttpRequest("https://jsonplaceholder.typicode.com/todos/1")
// {
//     Method = HttpMethod.GET
// };

// request.Headers.Add("some-header", "value");

// var response = Pdk.SendRequest(request);

// Pdk.SetOutput(response.Body);