//using System.Data.Common;
//using AltaSoft.Storm;
//using AltaSoft.Storm.Attributes;
//using AltaSoft.Storm.Interfaces;

//namespace TestProj;

///// <summary>
///// StormController for the FinalClass (.FinalClasses)
///// </summary>
//[StormController(default, "String", typeof(string))]
//public sealed class StringStormController : StormControllerBase
//{
//    public override IDataBindable Create(DbDataReader dr, uint partialLoadFlags, ref int idx)
//    {
//        return dr.GetString(idx++);
//    }

//    /// <summary>
//    /// Array of StormColumnDef objects representing the column definitions.
//    /// </summary>
//    internal static readonly StormColumnDef[] __columnDefs =
//    {
//        new("Value", null, "Value", StormColumnFlags.Key | StormColumnFlags.CanSelect | StormColumnFlags.CanInsert | StormColumnFlags.CanUpdate, UnifiedDbType.String, 0, 0, 0, SaveAs.Default, 0, false, null, null)
//    };

//    public override StormColumnDef[] ColumnDefs => __columnDefs;
//}
