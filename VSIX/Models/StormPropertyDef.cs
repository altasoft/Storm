#nullable enable

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AltaSoft.Storm.Models;

internal sealed class StormPropertyDef : INotifyPropertyChanged
{
    private readonly TypeGenerationSpec _typeSpec;
    private readonly string _flatObjectColumnPrefix;
    private string _bindingDisplayName;
    private StormPropertyStatus _status;
    private string? _statusMessage;
    private ImageMoniker _loadWithFlagsImage;
    private string _saveAsText;

    public string PropertyName { get; }

    public ITypeSymbol PropertyType { get; }

    public string PropertyTypeDisplayName => PropertyType.GetFullName() + (IsNullable ? "?" : "");

    public bool IsNullable { get; }

    public bool IsMasterDetailColumn { get; }

    public BindColumnData BindColumnData { get; }

    public StormTypeDef ParentStormType { get; internal set; } = default!;

    public BindColumnData GetEffectiveBindColumnData()
    {
        return new BindColumnData
        {
            ColumnName = BindColumnData.ColumnName ?? PropertyGenSpec.ColumnName,
            SaveAs = BindColumnData.SaveAs ?? PropertyGenSpec.SaveAs,
            LoadWithFlags = BindColumnData.LoadWithFlags ?? PropertyGenSpec.PartialLoadFlags != 0,
            DbType = BindColumnData.DbType is null or UnifiedDbType.Default ? PropertyGenSpec.DbType : BindColumnData.DbType.Value,
            Size = BindColumnData.Size ?? PropertyGenSpec.Size,
            Precision = BindColumnData.Precision ?? PropertyGenSpec.Precision,
            Scale = BindColumnData.Scale ?? PropertyGenSpec.Scale,
            ColumnType = BindColumnData.ColumnType ?? PropertyGenSpec.ColumnType,
            DetailTableName = BindColumnData.DetailTableName ?? PropertyGenSpec.GetDetailTableName(_typeSpec.TypeSymbol.GetFullName())
        };
    }

    public string BindingDisplayName
    {
        get => _bindingDisplayName;
        private set
        {
            if (value == _bindingDisplayName) return;
            _bindingDisplayName = value;
            OnPropertyChanged();
        }
    }

    public string SaveAsText
    {
        get => _saveAsText;
        private set
        {
            if (value.Equals(_saveAsText)) return;
            _saveAsText = value;
            OnPropertyChanged();
        }
    }

    public ImageMoniker LoadWithFlagsImage
    {
        get => _loadWithFlagsImage;
        private set
        {
            if (value.Equals(_loadWithFlagsImage)) return;
            _loadWithFlagsImage = value;
            OnPropertyChanged();
        }
    }

    public StormPropertyStatus Status
    {
        get => _status;
        private set
        {
            if (value == _status) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (value == _statusMessage) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<StormPropertyDef>? Details { get; set; }

    public PropertyGenerationSpec PropertyGenSpec { get; }

    public void SetStatus(StormPropertyStatus status, string message)
    {
        Status = status;
        StatusMessage = message;
    }

    //

    public bool IsKey => BindColumnData.ColumnType.HasValue && BindColumnData.ColumnType.Value.IsKey();

    public string GetColumnName() => _flatObjectColumnPrefix + (BindColumnData.ColumnName ?? PropertyGenSpec.ColumnName);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public StormPropertyDef(bool isMasterDetailColumn, PropertyGenerationSpec propSpec, TypeGenerationSpec typeSpec, string flatObjectColumnPrefix = "")
    {
        _typeSpec = typeSpec;
        PropertyGenSpec = propSpec;

        _flatObjectColumnPrefix = flatObjectColumnPrefix;
        IsMasterDetailColumn = isMasterDetailColumn;

        PropertyName = propSpec.PropertyName;
        PropertyType = propSpec.Property.Type;
        IsNullable = propSpec.IsNullable;

        BindColumnData = propSpec.BindColumnData;

        _status = StormPropertyStatus.NotChecked;

        _bindingDisplayName = GetBindingDisplayName();
        _saveAsText = GetSaveAsText();
        _loadWithFlagsImage = GetLoadWithFlagsImageMoniker();

        BindColumnData.PropertyChanged += (_, _) =>
        {
            BindingDisplayName = GetBindingDisplayName();
            SaveAsText = GetSaveAsText();
            LoadWithFlagsImage = GetLoadWithFlagsImageMoniker();
        }; // Calling setter of BindingDisplayName
    }

    public StormPropertyDef(bool isMasterDetailColumn, PropertyGenerationSpec propSpec, TypeGenerationSpec typeSpec, bool isNullable, string flatObjectColumnPrefix = "")
    {
        Debug.Assert(propSpec.ListItemTypeSymbol is not null);

        _typeSpec = typeSpec;
        PropertyGenSpec = propSpec;

        _flatObjectColumnPrefix = flatObjectColumnPrefix;
        IsMasterDetailColumn = isMasterDetailColumn;

        PropertyName = propSpec.ColumnName;
        PropertyType = propSpec.ListItemTypeSymbol!;
        IsNullable = isNullable;

        BindColumnData = new BindColumnData
        {
            ColumnName = propSpec.BindColumnData.ColumnName,
            ColumnType = DupColumnType.PrimaryKey,
            SaveAs = DupSaveAs.Default,
            LoadWithFlags = false,
            DbType = propSpec.BindColumnData.DbType,
            DetailTableName = propSpec.BindColumnData.DetailTableName,
            Size = propSpec.BindColumnData.Size,
            Precision = propSpec.BindColumnData.Precision,
            Scale = propSpec.BindColumnData.Scale
        };

        _status = StormPropertyStatus.NotChecked;

        _bindingDisplayName = GetBindingDisplayName();
        _saveAsText = GetSaveAsText();
        _loadWithFlagsImage = GetLoadWithFlagsImageMoniker();

        BindColumnData.PropertyChanged += (_, _) =>
        {
            BindingDisplayName = GetBindingDisplayName();
            SaveAsText = GetSaveAsText();
            LoadWithFlagsImage = GetLoadWithFlagsImageMoniker();
        }; // Calling setter of BindingDisplayName
    }

    private string GetSaveAsText()
    {
        var saveAs = BindColumnData.SaveAs ?? PropertyGenSpec.SaveAs;

        return saveAs switch
        {
            DupSaveAs.Default => "",
            DupSaveAs.String => "String",
            DupSaveAs.CompressedString => "CompressedString",
            DupSaveAs.Json => "Json",
            DupSaveAs.CompressedJson => "CompressedJson",
            DupSaveAs.Xml => "Xml",
            DupSaveAs.CompressedXml => "CompressedXml ",
            DupSaveAs.FlatObject => "Flat",
            DupSaveAs.DetailTable => "Det",
            _ => "-"
        };
    }

    private ImageMoniker GetLoadWithFlagsImageMoniker()
    {
        if (IsMasterDetailColumn || IsKey)
            return KnownMonikers.Key;

        return BindColumnData.LoadWithFlags ?? PropertyGenSpec.PartialLoadFlags != 0 ? KnownMonikers.FlagGroup : KnownMonikers.Blank;
    }

    private string GetBindingDisplayName()
    {
        var binding = GetEffectiveBindColumnData();

        var saveAs = binding.SaveAs;

        if (saveAs == DupSaveAs.FlatObject)
        {
            return GetColumnName();
        }

        var sb = new StringBuilder(64);

        if (saveAs == DupSaveAs.DetailTable)
        {
            sb.Append(binding.DetailTableName);
            return sb.ToString();
        }

        var size = binding.Size!.Value;
        var precision = binding.Precision!.Value;
        var scale = binding.Scale!.Value;
        var columnType = binding.ColumnType!.Value;
        var dbType = binding.DbType!.Value;

        sb.Append(GetColumnName().QuoteSqlName()).Append(' ').Append(dbType).Append(' ').Append(GetDbTypeDisplayString(dbType, size, precision, scale));

        sb.Append(IsNullable ? "" : " NOT").Append(" NULL");

        if (columnType != DupColumnType.Default)
        {
            sb.Append(' ').Append(columnType);
        }

        return sb.ToString();
    }

    // Same as in MsSqlOrmProvider
    private static string GetDbTypeDisplayString(UnifiedDbType dbType, int size, int precision, int scale)
    {
        return dbType switch
        {
            UnifiedDbType.AnsiString or UnifiedDbType.String or UnifiedDbType.AnsiStringFixedLength or UnifiedDbType.StringFixedLength or
                UnifiedDbType.AnsiText or UnifiedDbType.Text or UnifiedDbType.VarBinary or UnifiedDbType.Binary or UnifiedDbType.AnsiJson or UnifiedDbType.Json
                => size > 0 ? '(' + size.ToString(CultureInfo.InvariantCulture) + ')' : "(max)",

            UnifiedDbType.DateTime2
                => $"({(precision < 0 ? 7 : precision)})",
            UnifiedDbType.Time
                => $"({(precision < 0 ? 7 : precision)})",
            UnifiedDbType.DateTimeOffset
                => $"({(precision < 0 ? 7 : precision)})",

            UnifiedDbType.Decimal
                => $"({(precision < 0 ? 18 : precision)},{(scale < 0 ? 0 : scale)})",

            UnifiedDbType.Double
                => $"({(precision < 0 ? 53 : precision)})",

            _ => string.Empty
        };
    }
}
