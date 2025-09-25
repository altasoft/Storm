using System;

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Defines an interface for serialization providers, offering methods to serialize and deserialize objects to and from text representations.
/// </summary>
public interface IXmlSerializationProvider
{
    /// <summary>
    /// Serializes an object to its Xml representation.
    /// </summary>
    /// <param name="value">The object to be serialized.</param>
    /// <param name="typeToSerialize">Type to serialize current object or null</param>
    /// <returns>The Xml text representation of the object.</returns>
    string ToXml(object value, Type? typeToSerialize);

    /// <summary>
    /// Deserializes a Xml text representation back to its object form.
    /// </summary>
    /// <param name="xml">The Xml representation of the object.</param>
    /// <param name="returnType">The type to which the Xml text should be deserialized.</param>
    /// <returns>The deserialized object.</returns>
    object FromXml(string xml, Type returnType);
}
