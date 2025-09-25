using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Generator.Common;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.Models;

internal sealed class DbObjectDef : INotifyPropertyChanged
{
    public static readonly List<DbColumnDef> DummyColumns = [DbColumnDef.CreateDummy()];
    public static readonly List<DbIndexDef> DummyIndexes = [DbIndexDef.CreateDummy()];

    private List<DbColumnDef> _columns;
    private List<DbIndexDef> _indexes;

    /// <summary>
    /// ObjectId for the object
    /// </summary>
    public int Id { get; }
    public DupDbObjectType ObjectType { get; }
    public string SchemaName { get; set; }
    public string ObjectName { get; set; }

    public List<DbColumnDef> Columns
    {
        get => _columns;
        set
        {
            if (Equals(value, _columns)) return;
            _columns = value;
            OnPropertyChanged();
        }
    }

    public List<DbIndexDef> Indexes
    {
        get => _indexes;
        set
        {
            if (Equals(value, _indexes)) return;
            _indexes = value;
            OnPropertyChanged();
        }
    }

    public DbObjectStatus Status { get; set; }
    public string? StatusMessage { get; set; }

    public string Name => $"[{SchemaName}].[{ObjectName}]";

    public void SetStatus(DbObjectStatus status, string message)
    {
        Status = status;
        StatusMessage = message;
    }

    public DbObjectDef(SqlDataReader reader)
    {
        var type = (string)reader["type"];

        var dbEntityType = type.Trim() switch
        {
            "V" => DupDbObjectType.View,
            "TF" => DupDbObjectType.TableValuedFunction,
            "IF" => DupDbObjectType.TableValuedFunction,
            "P" => DupDbObjectType.StoredProcedure,
            "FN" => DupDbObjectType.ScalarValuedFunction,
            _ => DupDbObjectType.Table
        };

        ObjectType = dbEntityType;

        Id = (int)reader["object_id"];
        SchemaName = (string)reader["schema_name"];
        ObjectName = (string)reader["name"];
        Status = DbObjectStatus.Ok;

        _columns = DummyColumns; // We will fill this later
        _indexes = DummyIndexes; // We will fill this later
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
