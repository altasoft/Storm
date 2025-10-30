using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AltaSoft.Storm.Json;

/// <summary>
/// Converter to convert enums to and from strings and numbers.
/// </summary>
public sealed class JsonStringEnumConverterExt : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum && !typeToConvert.IsDefined(typeof(JsonConverterAttribute), false);
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new JsonStringEnumConverter().CreateConverter(typeToConvert, options);
    }
}
