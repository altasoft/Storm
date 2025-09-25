using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using AltaSoft.Storm.Generator.Common;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Models;

internal sealed class StormTypeDef : INotifyPropertyChanged
{
    private string _bindingDisplayName;
    private StormTypeStatus _status;
    private string? _statusMessage;

    public INamedTypeSymbol TypeSymbol { get; }
    public string TypeDisplayName => TypeSymbol.ToDisplayString();

    public BindObjectData BindObjectData { get; }

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

    public StormTypeStatus Status
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

    public ObservableCollection<StormPropertyDef> Properties { get; }

    public void SetStatus(StormTypeStatus status, string message)
    {
        Status = status;
        StatusMessage = message;
    }

    //

    public string GetObjectName() => BindObjectData.ObjectName ?? TypeSymbol.GetFullName().Pluralize();

    public string GetObjectNameWithSchema() => BindObjectData.SchemaName is null ? GetObjectName() : BindObjectData.SchemaName + '.' + GetObjectName();

    private string GetBindingDisplayName()
    {
        var sb = new StringBuilder();
        sb.Append(BindObjectData.SchemaName ?? "{schema}")
            .Append('.')
            .Append(GetObjectName())
            .Append(", UpdateMode: ")
            .Append(BindObjectData.UpdateMode);

        return sb.ToString();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public StormTypeDef(INamedTypeSymbol typeSymbol, BindObjectData bindObjectData, ObservableCollection<StormPropertyDef> properties)
    {
        TypeSymbol = typeSymbol;
        BindObjectData = bindObjectData;
        Properties = properties;

        _status = StormTypeStatus.Ok;

        _bindingDisplayName = GetBindingDisplayName();
        BindObjectData.PropertyChanged += (_, _) => BindingDisplayName = GetBindingDisplayName(); // Calling setter of BindingDisplayName
    }
}
