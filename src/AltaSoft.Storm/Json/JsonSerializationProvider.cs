using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Json;

/// <summary>
/// Provides JSON serialization and deserialization services, implementing the <see cref="IJsonSerializationProvider"/> interface.
/// This class uses the <see cref="JsonSerializer"/> for converting objects to and from JSON format.
/// </summary>
public class JsonSerializationProvider : IJsonSerializationProvider
{
    /// <summary>
    /// The default JSON serializer options used by the <see cref="JsonSerializationProvider"/>.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions = CreateDefaultSerializerOptions();

    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializationProvider"/> class with optional custom serializer options.
    /// </summary>
    /// <param name="serializerOptions">Optional. The JSON serializer options to use. If not provided, default options are used.</param>
    public JsonSerializationProvider(JsonSerializerOptions? serializerOptions = null)
    {
        _serializerOptions = serializerOptions ?? DefaultSerializerOptions;
    }

    /// <summary>
    /// Deserializes the provided JSON text to an object of the specified return type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="returnType">The type of the object to deserialize to.</param>
    /// <returns>An object of the specified type, deserialized from the JSON string.</returns>
    /// <exception cref="StormException">Thrown if the deserialization results in a null value for a non-nullable type.</exception>
    public object FromJson(string json, Type returnType)
    {
        return JsonSerializer.Deserialize(json, returnType, _serializerOptions)
            ?? throw new StormException("Null value for not null Json");
    }

    /// <summary>
    /// Serializes the provided object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="typeToSerialize">Type to serialize object to</param>
    /// <returns>A JSON string representation of the object.</returns>
    public string ToJson(object value, Type? typeToSerialize)
    {
        var type = typeToSerialize;

        if (type is null)
        {
            type = value.GetType();
            if (type.IsAbstract)
                type = typeof(object);
        }

        return JsonSerializer.Serialize(value, type, _serializerOptions);
    }

    private static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            AllowTrailingCommas = true,
            DictionaryKeyPolicy = null,
            IgnoreReadOnlyProperties = false,
            NumberHandling = JsonNumberHandling.Strict,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            ReferenceHandler = ReferenceHandler.IgnoreCycles
            //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        };

        options.Converters.Add(new JsonStringEnumConverterExt());
        return options;
    }
}
