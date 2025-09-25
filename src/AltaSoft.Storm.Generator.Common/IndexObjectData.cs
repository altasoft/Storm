using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AltaSoft.Storm.Generator.Common;

public sealed class IndexObjectData : INotifyPropertyChanged
{
    private string? _indexName;
    private string[] _indexColumns = [];
    private bool _isUnique;

    /// <summary>
    /// Represents the names of the columns in Index.
    /// </summary>
    public string[] IndexColumns
    {
        get => _indexColumns;
        set
        {
            if (_indexColumns.SequenceEqual(value, EqualityComparer<string>.Default))
                return;

            _indexColumns = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the index is unique.
    /// </summary>
    public bool IsUnique
    {
        get => _isUnique;
        set
        {
            if (_isUnique == value)
                return;

            _isUnique = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Represents the name of the index.
    /// </summary>
    public string? IndexName
    {
        get => _indexName;
        set
        {
            if (string.IsNullOrEmpty(value))
                value = null;
            if (string.Equals(value, _indexName, System.StringComparison.Ordinal)) return;

            _indexName = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
