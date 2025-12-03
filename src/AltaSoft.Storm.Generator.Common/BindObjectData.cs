using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AltaSoft.Storm.Generator.Common;

public sealed class BindObjectData : INotifyPropertyChanged
{
    private string _contextTypeName;
    private DupDbObjectType _objectType = DupDbObjectType.Table;
    private string? _schemaName;
    private string? _objectName;
    private DupUpdateMode? _updateMode;
    private string? _virtualViewSql;

    public BindObjectData(string contextTypeName)
    {
        _contextTypeName = contextTypeName;
    }

    /// <summary>
    /// Represents the type of the database object.
    /// </summary>
    public string ContextTypeName
    {
        get => _contextTypeName;
        set
        {
            if (string.Equals(value, _contextTypeName, System.StringComparison.Ordinal)) return;
            _contextTypeName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Represents the type of the database object.
    /// </summary>
    public DupDbObjectType ObjectType
    {
        get => _objectType;
        set
        {
            if (value == _objectType)
                return;

            _objectType = value;
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
    /// Represents whether changes to the table should be tracked.
    /// </summary>
    [DefaultValue(true)]
    public DupUpdateMode? UpdateMode
    {
        get => ObjectType is DupDbObjectType.Table or DupDbObjectType.CustomSqlStatement ? _updateMode : DupUpdateMode.NoUpdates;
        set
        {
            if (value == _updateMode)
                return;

            _updateMode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Represents the SQL statement for Virtual View.
    /// </summary>
    public string? VirtualViewSql
    {
        get => _virtualViewSql;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;
            if (string.Equals(value, _virtualViewSql, System.StringComparison.Ordinal)) return;

            _virtualViewSql = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the list of ParameterGenerationSpec objects.
    /// </summary>
    public List<ParameterGenerationSpec>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the display name of an object.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// If BulkInsert is true, then the generator will generate code for bulk copy operations.
    /// </summary>
    public bool BulkInsert { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
