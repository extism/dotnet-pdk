// https://github.com/emepetres/dotnet-wasm-sample

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace Extism.Pdk;

public class Pdk
{
    public static byte[] GetInput()
    {
        var length = Native.extism_input_length();
        if (length == 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[length];

        for (ulong i = 0; i < length; i++)
        {
            if (length - i >= 8)
            {
                var x = Native.extism_input_load_u64(i);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan((int)i), x);
                i += 7;
            }
            else
            {
                buffer[i] = Native.extism_input_load_u8(i);
            }
        }

        return buffer;
    }

    public static string GetInputString()
    {
        var bytes = GetInput();
        return Encoding.UTF8.GetString(bytes);
    }

    public static void SetOutput(MemoryBlock block)
    {
        Native.extism_output_set(block.Offset, block.Length);
    }

    public unsafe static void SetOutput(ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
        {
            var len = (ulong)data.Length;
            var offs = Native.extism_alloc(len);
            Native.extism_store(offs, ptr, len);
            Native.extism_output_set(offs, len);
        }
    }

    public static void SetOutput(string data)
    {
        SetOutput(Encoding.UTF8.GetBytes(data));
    }

    public static MemoryBlock Allocate(ulong length)
    {
        var offset = Native.extism_alloc(length);

        return new MemoryBlock(offset, length);
    }

    public static unsafe MemoryBlock Allocate(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0)
        {
            return MemoryBlock.Empty;
        }

        var block = Allocate((ulong)buffer.Length);

        fixed (byte* ptr = buffer)
        {
            Native.extism_store(block.Offset, ptr, (ulong)buffer.Length);
        }

        return block;
    }

    public static MemoryBlock Allocate(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return Allocate(bytes);
    }


    public static bool TryGetConfig(string key, [NotNullWhen(true)] out string? value)
    {
        value = null;

        var keyBlock = Allocate(key);

        var offset = Native.extism_config_get(keyBlock.Offset);
        var valueBlock = MemoryBlock.Find(offset);

        if (offset == 0 || valueBlock.Length == 0)
        {
            return false;
        }

        var bytes = valueBlock.ReadBytes();
        value = Encoding.UTF8.GetString(bytes);

        return true;
    }

    public static void Log(LogLevel level, MemoryBlock block)
    {
        switch (level)
        {
            case LogLevel.Info:
                Native.extism_log_info(block.Offset);
                break;

            case LogLevel.Debug:
                Native.extism_log_debug(block.Offset);
                break;

            case LogLevel.Warn:
                Native.extism_log_warn(block.Offset);
                break;

            case LogLevel.Error:
                Native.extism_log_error(block.Offset);
                break;
        }
    }

    public static void Log(LogLevel level, string message)
    {
        var block = Allocate(message);
        Log(level, block);
    }

    public static bool TryGetVar(string key, out MemoryBlock block)
    {
        var keyBlock = Allocate(key);

        var offset = Native.extism_var_get(keyBlock.Offset);
        block = MemoryBlock.Find(offset);

        if (offset == 0 || block.Length == 0)
        {
            return false;
        }

        return true;
    }

    public static void SetVar(string key, MemoryBlock value)
    {
        var keyBlock = Allocate(key);

        Native.extism_var_set(keyBlock.Offset, value.Offset);
    }

    public static void SetVar(string key, ReadOnlySpan<byte> bytes)
    {
        var block = Allocate(bytes);
        SetVar(key, block);
    }

    public static void RemoveVar(string key)
    {
        var keyBlock = Allocate(key);
        Native.extism_var_set(keyBlock.Offset, 0);
    }

    public static HttpResponse SendRequest(HttpRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("url", request.Url);
            writer.WriteString("method", request.Method);

            if (request.Headers.Count > 0)
            {
                writer.WriteStartObject("header");
                foreach (var kvp in request.Headers)
                {
                    writer.WriteString(kvp.Key, kvp.Value);
                }
                writer.WriteEndObject();
            }
        }

        var bytes = stream.ToArray();
        //Log(LogLevel.Error, "hi");

        // Log(LogLevel.Error, $"length: {bytes.Length}");

        // var requestBlock = Allocate(bytes);
        var bodyBlock = Allocate(request.Body);

        try
        {
            //var offset = Native.extism_http_request(requestBlock.Offset, bodyBlock.Offset);
            //var block = MemoryBlock.Find(offset);
            //var status = Native.extism_http_status_code();

            //return new HttpResponse(block, status);

            return new HttpResponse(new MemoryBlock(0, 0), 0);
        }
        finally
        {
            //requestBlock.Free();
            //bodyBlock.Free();
        }
    }

    //public static HttpClient GetHttpClient()
    //{
    //    return new HttpClient(new ExtismHttpMessageHandler());
    //}
}

public enum LogLevel
{
    Info,
    Debug,
    Warn,
    Error
}


//public class ExtismHttpMessageHandler : HttpMessageHandler
//{
//    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
//    {
//        if (request.RequestUri is null)
//        {
//            throw new InvalidOperationException("RequestUri can't be null.");
//        }

//        var extismHttpRequest = new HttpRequest
//        {
//            Url = request.RequestUri.AbsoluteUri,
//            Method = request.Method.Method
//        };

//        if (request.Content != null)
//        {
//            extismHttpRequest.Body = await request.Content.ReadAsByteArrayAsync();
//        }

//        foreach (var header in request.Headers)
//        {
//            extismHttpRequest.Headers.TryAdd(header.Key, string.Join(", ", header.Value));
//        }

//        var customResponse = Pdk.SendRequest(extismHttpRequest);

//        // Convert HttpResponse to HttpResponseMessage
//        var httpResponseMessage = new HttpResponseMessage((HttpStatusCode)customResponse.Status);

//        var memoryStream = new MemoryStream(customResponse.Body.ReadBytes());
//        httpResponseMessage.Content = new StreamContent(memoryStream);

//        return httpResponseMessage;
//    }
//}

public class HttpRequest
{
    public string Url { get; set; }
    public Dictionary<string, string> Headers { get; } = new();
    public string Method { get; set; } = "GET";
    public byte[] Body { get; set; } = Array.Empty<byte>();
}

public class HttpResponse : IDisposable
{
    public HttpResponse(MemoryBlock memory, ushort status)
    {
        Body = memory;
        Status = status;
    }

    public MemoryBlock Body { get; }
    public ushort Status { get; set; }

    public void Dispose()
    {
        Body.Free();
    }
}

public struct MemoryBlock
{
    public MemoryBlock(ulong offset, ulong length)
    {
        Offset = offset;
        Length = length;
    }

    public ulong Offset { get; }
    public ulong Length { get; }

    public bool IsEmpty => Length == 0;

    public static MemoryBlock Empty { get; } = new MemoryBlock(0, 0);

    public unsafe void CopyTo(Span<byte> buffer)
    {
        if ((ulong)buffer.Length < Length)
        {
            throw new InvalidOperationException($"Buffer must be at least ${Length} bytes.");
        }

        fixed (byte* ptr = buffer)
        {
            Native.extism_load(Offset, ptr, Length);
        }
    }

    public unsafe void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            Native.extism_store(Offset, ptr, Length);
        }
    }

    public byte[] ReadBytes()
    {
        var buffer = new byte[Length];
        CopyTo(buffer);

        return buffer;
    }

    public void Free()
    {
        if (!IsEmpty)
        {
            Native.extism_free(Offset);
        }
    }

    public static MemoryBlock Find(ulong offset)
    {
        var length = Native.extism_length(offset);
        return new MemoryBlock(offset, length);
    }
}