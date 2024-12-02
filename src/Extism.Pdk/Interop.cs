using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Extism;

/// <summary>
/// Provides interop functions for communication between guests and the host.
/// </summary>
public static class Pdk
{
    /// <summary>
    /// Read the input data sent by the host.
    /// </summary>
    /// <returns>The input data as a byte array.</returns>
    public static unsafe byte[] GetInput()
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
    /// Read the input data sent by the host as a UTF-8 encoded string.
    /// </summary>
    /// <returns></returns>
    public static string GetInputString()
    {
        var bytes = GetInput();
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Read the input data sent by the host as a UTF-8 encoded string and then deserialize it as JSON.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    public static T? GetInputJson<T>(JsonTypeInfo<T> typeInfo)
    {
        var json = GetInput();
        var reader = new Utf8JsonReader(json);

        return JsonSerializer.Deserialize(ref reader, typeInfo);
    }

    /// <summary>
    /// Set the output data to be sent back to the host.
    /// </summary>
    /// <param name="block">The memory block containing the output data.</param>
    public static void SetOutput(MemoryBlock block)
    {
        Native.extism_output_set(block.Offset, block.Length);
    }

    /// <summary>
    /// Set the output data to be sent back to the host as a byte buffer.
    /// </summary>
    /// <param name="data">The byte buffer to set as output data.</param>
    public static unsafe void SetOutput(ReadOnlySpan<byte> data)
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
    /// Set the output data to be sent back to the host as a UTF-8 encoded string.
    /// </summary>
    /// <param name="data">The UTF-8 encoded string to set as output data.</param>
    public static void SetOutput(string data)
    {
        SetOutput(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// Serialize the output data as JSON to be sent back to the host as a UTF-8 encoded string.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="output"></param>
    /// <param name="typeInfo"></param>
    public static void SetOutputJson<T>(T output, JsonTypeInfo<T> typeInfo)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        JsonSerializer.Serialize(writer, output, typeInfo);

        SetOutput(stream.ToArray());
    }

    /// <summary>
    /// Set plugin error
    /// </summary>
    /// <param name="errorMessage"></param>
    public static unsafe void SetError(string errorMessage)
    {
        var block = Allocate(errorMessage);
        Native.extism_error_set(block.Offset);
    }

    /// <summary>
    /// Allocate a block of memory with the specified length.
    /// </summary>
    /// <param name="length">The size of the memory block in bytes.</param>
    /// <returns>A <see cref="MemoryBlock"/> instance representing the allocated memory.</returns>
    public static MemoryBlock Allocate(ulong length)
    {
        var offset = Native.extism_alloc(length);
        if (offset == 0 && length > 0)
        {
            throw new InvalidOperationException("Failed to allocate memory block.");
        }

        return new MemoryBlock(offset, length);
    }

    /// <summary>
    /// Allocate a byte buffer into memory.
    /// </summary>
    /// <param name="buffer">The byte buffer to allocate into memory.</param>
    /// <returns>A <see cref="MemoryBlock"/> instance representing the allocated memory.</returns>
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
    /// Encodes a string as UTF-8 and stores it in memory.
    /// </summary>
    /// <param name="data">The string to allocate into memory.</param>
    /// <returns>A <see cref="MemoryBlock"/> instance representing the allocated memory.</returns>
    public static MemoryBlock Allocate(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return Allocate(bytes);
    }

    /// <summary>
    /// Try to get a configuration value from the host.
    /// </summary>
    /// <param name="key">The key for the configuration value.</param>
    /// <param name="value">The retrieved configuration value as a string.</param>
    /// <returns>True if the configuration value was retrieved successfully; otherwise, false.</returns>
    public static bool TryGetConfig(string key, [NotNullWhen(true)] out string value)
    {
        value = string.Empty;

        var keyBlock = Allocate(key);

        var offset = Native.extism_config_get(keyBlock.Offset);
        using var valueBlock = MemoryBlock.Find(offset);

        if (offset == 0 || valueBlock.Length == 0)
        {
            return false;
        }

        var bytes = valueBlock.ReadBytes();
        value = Encoding.UTF8.GetString(bytes);

        return true;
    }

    /// <summary>
    /// Log a message with the specified log level to the host.
    /// </summary>
    /// <param name="level">The log level for the message.</param>
    /// <param name="block">The memory block containing the log message.</param>
    public static void Log(LogLevel level, MemoryBlock block)
    {
        if (level < (LogLevel)Native.extism_get_log_level())
        {
            return;
        }

        switch (level)
        {
            case LogLevel.Trace:
                Native.extism_log_trace(block.Offset);
                break;

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
        if (level < (LogLevel)Native.extism_get_log_level())
        {
            return;
        }

        var block = Allocate(message);
        Log(level, block);
    }

    /// <summary>
    /// Read a var that's persisted by the host. See <see cref="SetVar(string, MemoryBlock)"/>
    /// </summary>
    /// <param name="key">The variable name.</param>
    /// <param name="block">The value of the variable. The plugin should take ownership of the block and free it after reading the data.</param>
    /// <returns>true if the variable is found, false otherwise.</returns>
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
    /// Set a var that will be persisted by the host.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void SetVar(string key, MemoryBlock value)
    {
        var keyBlock = Allocate(key);

        Native.extism_var_set(keyBlock.Offset, value.Offset);
    }

    /// <summary>
    /// Set a variable value persisted by the host.
    /// </summary>
    /// <param name="key">The key for the persisted variable.</param>
    /// <param name="bytes">The byte buffer to set as the variable value.</param>
    public static void SetVar(string key, ReadOnlySpan<byte> bytes)
    {
        var block = Allocate(bytes);
        SetVar(key, block);
    }

    /// <summary>
    /// Set a variable value persisted by the host.
    /// </summary>
    /// <param name="key">The key for the persisted variable.</param>
    /// <param name="value">A string value that will be UTF8 encoded.</param>
    public static void SetVar(string key, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        SetVar(key, bytes);
    }

    /// <summary>
    /// Remove a variable from host memory.
    /// </summary>
    /// <param name="key">The key of the variable to remove.</param>
    public static void RemoveVar(string key)
    {
        var keyBlock = Allocate(key);
        Native.extism_var_set(keyBlock.Offset, 0);
    }

    /// <summary>
    /// Send an HTTP request synchronously and get the response from the host.
    /// </summary>
    /// <param name="request">The HTTP request to send.</param>
    /// <returns>The HTTP response received from the host. The plugin takes ownership of the memory block and is expected to free it.</returns>
    public static HttpResponse SendRequest(HttpRequest request)
    {
        var requestJson = JsonSerializer.Serialize(request, JsonContext.Default.HttpRequest);

        using var requestBlock = Allocate(requestJson);
        using var bodyBlock = Allocate(request.Body);

        var responseOffset = Native.extism_http_request(requestBlock.Offset, bodyBlock.Offset);
        if (responseOffset == 0)
        {
            throw new InvalidOperationException("Failed to send HTTP request.");
        }

        var responseBody = MemoryBlock.Find(responseOffset);
        var status = Native.extism_http_status_code();
        var httpResponse = new HttpResponse(responseBody, status);

        var headersOffset = Native.extism_http_headers();
        if (headersOffset > 0)
        {
            using var headersBlock = MemoryBlock.Find(headersOffset);
            var headersJson = headersBlock.ReadString();

            httpResponse.Headers = JsonSerializer.Deserialize(headersJson, JsonContext.Default.DictionaryStringString) ?? [];
        }

        return httpResponse;
    }
}

/// <summary>
/// Log level
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level logging 
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Debug level logging
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Information level logging
    /// </summary>
    Info = 2,

    /// <summary>
    /// Warning level logging
    /// </summary>
    Warn = 3,

    /// <summary>
    /// Error level logging
    /// </summary>
    Error = 4,
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
    }

    /// <summary>
    /// HTTP URL
    /// </summary>
    [JsonPropertyName("url")]
    public Uri Url { get; set; }

    /// <summary>
    /// HTTP Headers
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// HTTP method
    /// </summary>
    [JsonPropertyName("method")]
    [JsonConverter(typeof(JsonStringEnumConverter<HttpMethod>))]
    public HttpMethod Method { get; set; } = HttpMethod.GET;

    /// <summary>
    /// An optional body.
    /// </summary>
    [JsonPropertyName("body")]
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
    /// HTTP Headers. Make sure HTTP response headers are enabled in the host.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Frees the current memory block.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // free managed resources
        }

        Body.Dispose();
    }
}

/// <summary>
/// A block of allocated memory.
/// </summary>
public class MemoryBlock : IDisposable
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
        CheckDisposed();

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
        CheckDisposed();

        if((ulong)bytes.Length > Length)
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
        CheckDisposed();

        var buffer = new byte[Length];
        CopyTo(buffer);

        return buffer;
    }

    /// <summary>
    /// Reads the current memory block as a UTF8 encoded string.
    /// </summary>
    /// <returns></returns>
    public string ReadString()
    {
        var bytes = ReadBytes();
        return Encoding.UTF8.GetString(bytes);
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

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MemoryBlock));
        }
    }

    private bool _disposed;
    /// <summary>
    /// Frees the current memory block.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            // free managed resources
        }

        if (!IsEmpty)
        {
            Native.extism_free(Offset);
        }
    }
}

[JsonSerializable(typeof(HttpRequest))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class JsonContext : JsonSerializerContext
{

}