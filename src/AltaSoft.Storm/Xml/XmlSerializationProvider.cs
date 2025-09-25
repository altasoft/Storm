using System;
using System.IO;
using System.Xml.Serialization;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Xml;

/// <summary>
/// Provides Xml serialization and deserialization services, implementing the <see cref="IXmlSerializationProvider"/> interface.
/// This class uses the <see cref="XmlSerializer"/> for converting objects to and from Xml format.
/// </summary>
public class XmlSerializationProvider : IXmlSerializationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlSerializationProvider"/> class.
    /// </summary>
    public XmlSerializationProvider()
    {
    }

    /// <summary>
    /// Deserializes the provided Xml text to an object of the specified return type.
    /// </summary>
    /// <param name="xml">The Xml string to deserialize.</param>
    /// <param name="returnType">The type of the object to deserialize to.</param>
    /// <returns>An object of the specified type, deserialized from the Xml string.</returns>
    /// <exception cref="StormException">Thrown if the deserialization results in a null value for a non-nullable type.</exception>
    public object FromXml(string xml, Type returnType)
    {
        var xmlSerializer = new XmlSerializer(returnType);
        using var reader = new StringReader(xml);

        return xmlSerializer.Deserialize(reader) ?? throw new StormException("Null value for not null Xml");
    }

    ///// <summary>
    ///// Deserializes XML data from an XmlReader into an object of the specified return type.
    ///// Throws a StormException if the deserialized value is null when it is expected to be not null.
    ///// </summary>
    ///// <param name="xmlReader">The XmlReader containing the XML data to deserialize.</param>
    ///// <param name="returnType">The type of the object to return after deserialization.</param>
    ///// <returns>
    ///// The deserialized object of the specified return type.
    ///// </returns>
    //public object FromXmlReader(XmlReader xmlReader, Type returnType)
    //{
    //    var xmlSerializer = new XmlSerializer(returnType);

    //    return xmlSerializer.Deserialize(xmlReader) ?? throw new StormException("Null value for not null Xml");
    //}

    /// <summary>
    /// Serializes the provided object to a Xml string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <param name="typeToSerialize">Type to serialize object to</param>
    /// <returns>A Xml string representation of the object.</returns>
    public string ToXml(object value, Type? typeToSerialize)
    {
        var type = typeToSerialize;
        if (type is null)
        {
            type = value.GetType();
            if (type.IsAbstract)
                type = typeof(object);
        }

        var xmlSerializer = new XmlSerializer(type);
        using var writer = new Utf8StringWriter();
        xmlSerializer.Serialize(writer, value);

        return writer.ToString();
    }

    ///// <summary>
    ///// Serializes an object to XML and writes it to an XmlWriter.
    ///// </summary>
    //public void ToXmlWriter(object value, XmlWriter xmlWriter)
    //{
    //    var type = value.GetType();
    //    if (type.IsAbstract)
    //        type = typeof(object);

    //    var xmlSerializer = new XmlSerializer(type);
    //    xmlSerializer.Serialize(xmlWriter, value);
    //}
}
