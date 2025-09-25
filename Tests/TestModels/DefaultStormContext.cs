//#pragma warning disable CA1822

//// ReSharper disable UnusedParameter.Local
//// ReSharper disable UnusedMember.Local
//#pragma warning disable IDE0060, IDE0051

//namespace AltaSoft.Storm.TestModels;

////public static class ModuleInitializerTest
////{
////    [ModuleInitializer]
////    public static void Initialize()
////    {
////        Console.WriteLine("Module initializer");
////    }
////}

//public partial class DefaultStormContext : StormContext
//{
//    public DefaultStormContext(string connectionString) : base(connectionString)
//    {
//        //using (var scope = new TransactionScope(TransactionScopeOption.Required,
//        //           new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
//        //{
//        //}
//    }
//}

////public partial class TestStormContext
////{
////    [MethodImpl(MethodImplOptions.AggressiveInlining)]
////    public User.ISelectFromUser SelectFromUser() => new User.SelectFromUser(GetConnection());
////    public User.ISelectFromUserSingle SelectFromUser(int userId, short branchId) => new User.SelectFromUserSingle(GetConnection(), [userId, branchId]);
////    public User.IDeleteFromUser DeleteFromUser() => new User.DeleteFromUser(GetConnection());
////    public User.IDeleteFromUserSingle DeleteFromUser(int userId, short branchId) => new User.DeleteFromUserSingle(GetConnection(), keyValues: [userId, branchId]);
////    public User.IDeleteFromUserSingle DeleteFromUser(User value) => new User.DeleteFromUserSingle(GetConnection(), value: value);
////    public User.IDeleteFromUserSingle DeleteFromUser(IEnumerable<User> values) => new User.DeleteFromUserSingle(GetConnection(), values: values);
////    public User.IUpdateFromUser UpdateUser() => new User.UpdateFromUser(GetConnection());
////    public User.IUpdateFromSingleUser UpdateUser(int userId, short branchId) => new User.UpdateFromSingleUser(GetConnection(), keyValues: [userId, branchId]);
////    public User.IInsertIntoUser InsertIntoUser() => new User.InsertIntoUser(GetConnection());

////}
