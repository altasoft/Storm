using System.IO;
using System.Text;

namespace AltaSoft.Storm.Xml;

/// <summary>
/// Represents a StringWriter that uses UTF-8 encoding.
/// </summary>
internal sealed class Utf8StringWriter : StringWriter
{
    /// <inheritdoc/>
    public override Encoding Encoding => Encoding.UTF8;
}
