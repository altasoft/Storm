using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AltaSoft.Storm.Generator.Common;

public sealed class BindColumnData : INotifyPropertyChanged
{
    private string? _columnName;
    private DupSaveAs? _saveAs;
    private bool? _loadWithFlags;
    private UnifiedDbType? _dbType;
    private int? _size;
    private int? _precision;
    private int? _scale;
    private DupColumnType? _columnType;
    private string? _detailTableName;

    public string? ColumnName
    {
        get => _columnName;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;

            if (string.Equals(value, _columnName, System.StringComparison.Ordinal)) return;
            _columnName = value;
            OnPropertyChanged();
        }
    }

    [DefaultValue(DupSaveAs.Default)]
    public DupSaveAs? SaveAs
    {
        get => _saveAs;
        set
        {
            if (value == _saveAs) return;
            _saveAs = value;
            OnPropertyChanged();
        }
    }

    [DefaultValue(false)]
    public bool? LoadWithFlags
    {
        get => _loadWithFlags;
        set
        {
            if (value == _loadWithFlags) return;
            _loadWithFlags = value;
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

    [DefaultValue(DupColumnType.Default)]
    public DupColumnType? ColumnType
    {
        get => _columnType;
        set
        {
            if (value == _columnType) return;
            _columnType = value;
            OnPropertyChanged();
        }
    }

    public string? DetailTableName
    {
        get => _detailTableName;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;

            if (string.Equals(value, _detailTableName, System.StringComparison.Ordinal)) return;
            _detailTableName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Determines if the column is a primary key based on its column type.
    /// </summary>
    /// <returns>True if the column is a primary key, false otherwise.</returns>
    public bool IsKey => ColumnType.HasValue && ((ColumnType.Value & DupColumnType.PrimaryKey) != DupColumnType.Default);

    /// <summary>
    /// Gets a value indicating whether the column has a concurrency check.
    /// </summary>
    /// <returns>True if the column has a concurrency check; otherwise, false.</returns>
    public bool IsConcurrencyCheck => ColumnType.HasValue && ((ColumnType.Value & DupColumnType.ConcurrencyCheck) != 0);

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
