using System;

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Defines an interface for serialization providers, offering methods to serialize and deserialize objects to and from text representations.
/// </summary>
public interface IJsonSerializationProvider
{
    /// <summary>
    /// Serializes an object to its Json representation.
    /// </summary>
    /// <param name="value">The object to be serialized.</param>
    /// <param name="typeToSerialize">Type to serialize current object or null</param>
    /// <returns>The Json text representation of the object.</returns>
    string ToJson(object value, Type? typeToSerialize);

    /// <summary>
    /// Deserializes a Json text representation back to its object form.
    /// </summary>
    /// <param name="json">The Json representation of the object.</param>
    /// <param name="returnType">The type to which the Json text should be deserialized.</param>
    /// <returns>The deserialized object.</returns>
    object FromJson(string json, Type returnType);
}
