using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

namespace AltaSoft.Storm.Generator.Common;

public sealed class BindParameterData : INotifyPropertyChanged
{
    private string? _parameterName;
    private UnifiedDbType? _dbType;
    private int? _size;
    private int? _precision;
    private int? _scale;
    private ParameterDirection? _direction;
    private string? _schemaName;
    private string? _objectName;

    public string? ParameterName
    {
        get => _parameterName;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;

            if (string.Equals(value, _parameterName, System.StringComparison.Ordinal)) return;
            _parameterName = value;
            OnPropertyChanged();
        }
    }

    [DefaultValue(UnifiedDbType.Default)]
    public UnifiedDbType? DbType
    {
        get => _dbType;
        set
        {
            if (value == _dbType) return;
            _dbType = value;
            OnPropertyChanged();
        }
    }

    public int? Size
    {
        get => _size;
        set
        {
            if (value == _size) return;
            _size = value;
            OnPropertyChanged();
        }
    }

    public int? Precision
    {
        get => _precision;
        set
        {
            if (value == _precision) return;
            _precision = value;
            OnPropertyChanged();
        }
    }

    public int? Scale
    {
        get => _scale;
        set
        {
            if (value == _scale) return;
            _scale = value;
            OnPropertyChanged();
        }
    }

    public ParameterDirection? Direction
    {
        get => _direction;
        set
        {
            if (value == _direction) return;
            _direction = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Represents the name of the schema.
    /// </summary>
    public string? SchemaName
    {
        get => _schemaName;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;
            if (string.Equals(value, _schemaName, System.StringComparison.Ordinal)) return;

            _schemaName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Represents the name of the table.
    /// </summary>
    public string? ObjectName
    {
        get => _objectName;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;
            if (string.Equals(value, _objectName, System.StringComparison.Ordinal)) return;

            _objectName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Event that is raised when a property value changes.
    /// </summary>
    /// <remarks>
    /// The event handler should be of type PropertyChangedEventHandler.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event with the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
