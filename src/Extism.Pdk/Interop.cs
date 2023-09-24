// https://github.com/emepetres/dotnet-wasm-sample

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace Extism;

/// <summary>
/// Interop functions that allow guests to communicate with the host.
/// </summary>
public static class Pdk
{
    /// <summary>
    /// Read call input.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Read call input as a UTF8 string.
    /// </summary>
    /// <returns></returns>
    public static string GetInputString()
    {
        var bytes = GetInput();
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Set call output.
    /// </summary>
    /// <param name="block"></param>
    public static void SetOutput(MemoryBlock block)
    {
        Native.extism_output_set(block.Offset, block.Length);
    }

    /// <summary>
    /// Set call output to a byte buffer.
    /// </summary>
    /// <param name="data"></param>
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

    /// <summary>
    /// Set call output to a UTF8 string.
    /// </summary>
    /// <param name="data"></param>
    public static void SetOutput(string data)
    {
        SetOutput(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// Allocate a block of memory.
    /// </summary>
    /// <param name="length">Block size in bytes</param>
    /// <returns></returns>
    public static MemoryBlock Allocate(ulong length)
    {
        var offset = Native.extism_alloc(length);

        return new MemoryBlock(offset, length);
    }

    /// <summary>
    /// Allocate an byte buffer into memory.
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Allocate a string into memory using UTF8 encoding.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static MemoryBlock Allocate(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return Allocate(bytes);
    }

    /// <summary>
    /// Tries to get a configuration from the host.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetConfig(string key, out string value)
    {
        value = string.Empty;

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

    /// <summary>
    /// Logs a message to the host.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="block"></param>
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

    /// <summary>
    /// Logs a message to the host.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    public static void Log(LogLevel level, string message)
    {
        var block = Allocate(message);
        Log(level, block);
    }

    /// <summary>
    /// Tries to get a var persisted by the host.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="block"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Tries to set a var that will be persisted by the host.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void SetVar(string key, MemoryBlock value)
    {
        var keyBlock = Allocate(key);

        Native.extism_var_set(keyBlock.Offset, value.Offset);
    }

    /// <summary>
    ///  Tries to set a var that will be persisted by the host.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bytes"></param>
    public static void SetVar(string key, ReadOnlySpan<byte> bytes)
    {
        var block = Allocate(bytes);
        SetVar(key, block);
    }

    /// <summary>
    /// Remove a var from host memory.
    /// </summary>
    /// <param name="key"></param>
    public static void RemoveVar(string key)
    {
        var keyBlock = Allocate(key);
        Native.extism_var_set(keyBlock.Offset, 0);
    }

    /// <summary>
    /// Send an HTTP request synchronously and get the response back.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static HttpResponse SendRequest(HttpRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("url", request.Url.AbsoluteUri);
            writer.WriteString("method", Enum.GetName(typeof(HttpMethod), request.Method));

            if (request.Headers.Count > 0)
            {
                writer.WriteStartObject("header");
                foreach (var kvp in request.Headers)
                {
                    writer.WriteString(kvp.Key, kvp.Value);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        var bytes = stream.ToArray();

        var requestBlock = Allocate(bytes);
        var bodyBlock = Allocate(request.Body);

        var offset = Native.extism_http_request(requestBlock.Offset, bodyBlock.Offset);
        var block = MemoryBlock.Find(offset);
        var status = Native.extism_http_status_code();

        return new HttpResponse(block, status);
    }
}

/// <summary>
/// Log level
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Information
    /// </summary>
    Info,

    /// <summary>
    /// Debug
    /// </summary>
    Debug,

    /// <summary>
    /// Warning
    /// </summary>
    Warn,

    /// <summary>
    /// Error
    /// </summary>
    Error
}

/// <summary>
/// An HTTP request
/// </summary>
public class HttpRequest
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    public HttpRequest(Uri url)
    {
        Url = url;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    public HttpRequest(string url)
    {
        Url = new Uri(url);
        Pdk.Log(LogLevel.Error, url);
    }

    /// <summary>
    /// HTTP URL
    /// </summary>
    public Uri Url { get; set; }

    /// <summary>
    /// HTTP Headers
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// HTTP method
    /// </summary>
    public HttpMethod Method { get; set; } = HttpMethod.GET;

    /// <summary>
    /// An optional body.
    /// </summary>
    public byte[] Body { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// HTTP Method
/// </summary>
public enum HttpMethod
{
    /// <summary>
    /// GET
    /// </summary>
    GET,

    /// <summary>
    /// POST
    /// </summary>
    POST,

    /// <summary>
    /// PUT
    /// </summary>
    PUT,

    /// <summary>
    /// DELETE
    /// </summary>
    DELETE,

    /// <summary>
    /// HEAD
    /// </summary>
    HEAD,

    /// <summary>
    /// PATCH
    /// </summary>
    PATCH,
}

/// <summary>
/// Response from an HTTP call
/// </summary>
public class HttpResponse : IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="memory"></param>
    /// <param name="status"></param>
    public HttpResponse(MemoryBlock memory, ushort status)
    {
        Body = memory;
        Status = status;
    }

    /// <summary>
    /// Body of the HTTP response
    /// </summary>
    public MemoryBlock Body { get; }

    /// <summary>
    /// HTTP Status Code
    /// </summary>
    public ushort Status { get; set; }

    /// <summary>
    /// Frees up the body of the HTTP response.
    /// </summary>
    public void Dispose()
    {
        Body.Free();
    }
}

/// <summary>
/// A block of allocated memory.
/// </summary>
public struct MemoryBlock
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    public MemoryBlock(ulong offset, ulong length)
    {
        Offset = offset;
        Length = length;
    }

    /// <summary>
    /// Starts address of block
    /// </summary>
    public ulong Offset { get; }

    /// <summary>
    /// Length of block in bytes
    /// </summary>
    public ulong Length { get; }

    /// <summary>
    /// Is block empty.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// An Empty memory block.
    /// </summary>
    public static MemoryBlock Empty { get; } = new MemoryBlock(0, 0);

    /// <summary>
    /// Copies the contents of a memory block into a buffer.
    /// </summary>
    /// <param name="buffer"></param>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// Writes a string to the current memory block.
    /// </summary>
    /// <param name="text"></param>
    public void WriteString(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        WriteBytes(bytes);
    }

    /// <summary>
    /// Writes a byte buffer to this memory block.
    /// </summary>
    /// <param name="bytes"></param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public unsafe void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if ((ulong)bytes.Length > Length)
        {
            throw new IndexOutOfRangeException("Memory block is not big enough.");
        }

        fixed (byte* ptr = bytes)
        {
            Native.extism_store(Offset, ptr, Length);
        }
    }

    /// <summary>
    /// Reads the current memory block as a byte array.
    /// </summary>
    /// <returns></returns>
    public byte[] ReadBytes()
    {
        var buffer = new byte[Length];
        CopyTo(buffer);

        return buffer;
    }

    /// <summary>
    /// Frees the current memory block.
    /// </summary>
    public void Free()
    {
        if (!IsEmpty)
        {
            Native.extism_free(Offset);
        }
    }

    /// <summary>
    /// Finds a memory block based on its start address.
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    public static MemoryBlock Find(ulong offset)
    {
        var length = Native.extism_length(offset);
        return new MemoryBlock(offset, length);
    }
}