using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Extism;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public interface IExtismSerializer
{
    public T? Deserialize<T>(byte[] data, JsonTypeInfo typeInfo);
    public byte[] Serialize(object payload, JsonTypeInfo typeInfo);
}

public class JsonExtismSerializer : IExtismSerializer
{
    public T? Deserialize<T>(byte[] data, JsonTypeInfo typeInfo)
    {
        var reader = new Utf8JsonReader(data);
        return (T?)JsonSerializer.Deserialize(ref reader, typeInfo);
    }

    public byte[] Serialize(object payload, JsonTypeInfo typeInfo)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        JsonSerializer.Serialize(writer, payload, typeInfo);

        return stream.ToArray();
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class JsonInputOutputAttribute : Attribute
{
    public JsonInputOutputAttribute(Type context)
    {
        Context = context;
    }

    public Type Context { get; }
}


///// <summary>
///// 
///// </summary>
//public interface IExtismSerializable
//{
//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="serializationOptions"></param>
//    /// <returns></returns>
//    public byte[] Serialize(object serializationOptions);

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="serializationOptions"></param>
//    /// <returns></returns>
//    public object Deserialize(object serializationOptions);
//}

///// <summary>
///// 
///// </summary>
///// <typeparam name="T"></typeparam>
//public class Json<T> : IExtismSerializable
//{
//    /// <summary>
//    /// 
//    /// </summary>
//    public Json(T payload)
//    {
//        Payload = payload;
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    public T Payload { get; set; }

//    /// <summary>
//    /// 
//    /// </summary>
//    public byte[] Serialize(object serializationOptions)
//    {
//        throw new NotImplementedException();
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    public object Deserialize(object serializationOptions)
//    {
//        throw new NotImplementedException();
//    }
//}