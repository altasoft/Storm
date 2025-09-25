// ReSharper disable RedundantUsingDirective.Global
#pragma warning disable IDE0005 // Using directive is unnecessary.

#if STORM_MSSQL
#if !STORM_ALIASES_DEFINED
#define STORM_ALIASES_DEFINED

//global using UnifiedDbType = System.Data.SqlDbType;
global using StormDbParameter = Microsoft.Data.SqlClient.SqlParameter;
global using StormDbCommand = Microsoft.Data.SqlClient.SqlCommand;
global using StormDbConnection = Microsoft.Data.SqlClient.SqlConnection;
global using StormDbTransaction = Microsoft.Data.SqlClient.SqlTransaction;
global using StormDbDataReader = Microsoft.Data.SqlClient.SqlDataReader;
global using StormDbException = Microsoft.Data.SqlClient.SqlException;
global using StormDbParameterCollection = Microsoft.Data.SqlClient.SqlParameterCollection;
global using StormNativeDbType = System.Data.SqlDbType;

#if NET6_0_OR_GREATER
global using StormDbBatchCommand = Microsoft.Data.SqlClient.SqlBatchCommand;
global using StormDbBatch = Microsoft.Data.SqlClient.SqlBatch;
#endif

#endif
#endif
