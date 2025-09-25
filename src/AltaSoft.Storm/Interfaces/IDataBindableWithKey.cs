using System.ComponentModel;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Represents an interface for data bindable objects with a key.
/// </summary>
public interface IDataBindableWithKey : IDataBindable
{
    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Retrieves the key value of an object.
    /// </summary>
    /// <returns>The key value of the object.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    object __GetKeyValue();
}
