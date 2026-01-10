using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Interfaces;
using AltaSoft.Storm.Json;
using AltaSoft.Storm.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace AltaSoft.Storm;

/// <summary>
/// Static class that provides configuration and initialization for the ORM framework.
/// </summary>
public static class StormManager
{
    private static readonly ObjectPool<StringBuilder> s_stringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

    /// <summary>
    /// Gets a value indicating whether the StormManager is initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the provider for the Object-Relational Mapping (ORM).
    /// </summary>
    /// <value>The ORM provider.</value>
    public static IOrmProvider Provider { get; private set; } = default!;

    /// <summary>
    /// Gets the SQL dialect used by this provider.
    /// </summary>
    /// <value>The SQL dialect.</value>
    public static string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the character used for quoting identifiers in SQL statements.
    /// </summary>
    /// <value>The quote character.</value>
    public static char QuoteCharacter { get; private set; } = '[';

    /// <summary>
    /// Gets the maximum length of system names like table and column names.
    /// </summary>
    /// <value>The maximum system name length.</value>
    public static int MaxSysNameLength { get; private set; } = 128;

    /// <summary>
    /// Gets or sets the ILogger instance. Can be null.
    /// </summary>
    public static ILogger? Logger { get; private set; }

    /// <summary>
    /// Indicates whether the configured logger is enabled at Trace level. Cached to avoid repeated checks.
    /// </summary>
    public static bool IsTraceEnabled { get; private set; }

    /// <summary>
    /// Gets the delegate used to create a command.
    /// </summary>
    /// <value>The create command delegate.</value>
    public static CreateCommandDelegate CreateCommand { get; private set; } = (_) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the delegate used to create a batch command.
    /// </summary>
    /// <value>The create batch command delegate.</value>
    public static CreateBatchCommandDelegate CreateBatchCommand { get; private set; } = (_) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets a function that converts a <see cref="UnifiedDbType"/> to a string representation of a UnifiedDbType
    /// </summary>
    /// <value>
    /// The function that converts a UnifiedDbType to a string representation of a SqlDbType.
    /// </value>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    public static ToSqlDbTypeDelegate ToSqlDbType { get; private set; } = (_, _, _, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the function that adds a parameter to a <see cref="StormDbCommand"/> for SQL Server, handling conversion of certain .NET types and value adjustments.
    /// </summary>
    /// <value>The function used to add a database parameter to a DbCommand.</value>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    internal static AddDbParameterDelegate AddDbParameter { get; private set; } = (_, _, _, _, _, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the function that adds a parameter to a <see cref="StormDbBatchCommand"/> for SQL Server, handling conversion of certain .NET types and value adjustments.
    /// </summary>
    /// <value>The function used to add a database batch parameter to a DbBatchCommand.</value>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    internal static AddDbBatchParameterDelegate AddDbBatchParameter { get; private set; } = (_, _, _, _, _, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the delegate used to convert an object to its Json text representation.
    /// </summary>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    public static ToTextDelegate ToJson { get; private set; } = (_, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the delegate used to convert a Json string to an object of the specified type.
    /// </summary>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    public static FromTextDelegate FromJson { get; private set; } = (_, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the delegate used to convert an object to its Xml text representation.
    /// </summary>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    public static ToTextDelegate ToXml { get; private set; } = (_, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the delegate used to convert a Xml string to an object of the specified type.
    /// </summary>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    public static FromTextDelegate FromXml { get; private set; } = (_, _) => throw new StormException("Not initialized");

    /// <summary>
    /// Gets the delegate used to handle exceptions that occur during database operations
    /// </summary>
    /// <exception cref="StormException">Thrown if the function is not initialized.</exception>
    public static HandleDbExceptionDelegate HandleDbException { get; private set; } = (_) => throw new StormException("Not initialized");

    /// <summary>
    /// Retrieves a StringBuilder instance from the string builder pool.
    /// </summary>
    public static StringBuilder GetStringBuilderFromPool() => s_stringBuilderPool.Get();

    /// <summary>
    /// Returns a StringBuilder instance to the pool for reuse.
    /// </summary>
    public static void ReturnStringBuilderToPool(StringBuilder sb) => s_stringBuilderPool.Return(sb);

    /// <summary>
    /// Converts the content of the StringBuilder to a string, returns the string, and then returns the StringBuilder to the pool for reuse.
    /// </summary>
    /// <returns>
    /// The string representation of the content in the StringBuilder before returning it to the pool.
    /// </returns>
    public static string ToStringAndReturnToPool(this StringBuilder sb)
    {
        var result = sb.ToString();
        s_stringBuilderPool.Return(sb);
        return result;
    }

    /// <summary>
    /// Initializes the ORM provider, serialization provider, and logger for the application.
    /// </summary>
    /// <param name="ormProvider">The ORM provider to use.</param>
    /// <param name="configuration">The action to configure Storm using a <see cref="StormConfiguration"/> instance.</param>
    /// <param name="jsonSerializationProvider">The Json serialization provider to use.</param>
    /// <param name="xmlSerializationProvider">The Xml serialization provider to use.</param>
    /// <param name="logger">The logger to use (optional).</param>
    public static void Initialize(
        IOrmProvider ormProvider,
        Action<StormConfiguration> configuration,
        IJsonSerializationProvider? jsonSerializationProvider = null,
        IXmlSerializationProvider? xmlSerializationProvider = null,
        ILogger? logger = null)
    {
        if (IsInitialized)
            throw new StormException($"The {nameof(StormManager)} has already been initialized.");

        Provider = ormProvider;

        Description = ormProvider.Description;
        QuoteCharacter = ormProvider.QuoteCharacter;
        MaxSysNameLength = ormProvider.MaxSysNameLength;

        CreateCommand = ormProvider.CreateCommand;
        CreateBatchCommand = ormProvider.CreateBatchCommand;

        ToSqlDbType = ormProvider.ToSqlDbType;
        HandleDbException = ormProvider.HandleDbException;

        AddDbParameter = ormProvider.AddDbParameter;
        AddDbBatchParameter = ormProvider.AddDbParameter;

        jsonSerializationProvider ??= new JsonSerializationProvider();
        xmlSerializationProvider ??= new XmlSerializationProvider();

        ToJson = jsonSerializationProvider.ToJson;
        FromJson = jsonSerializationProvider.FromJson;

        ToXml = xmlSerializationProvider.ToXml;
        FromXml = xmlSerializationProvider.FromXml;

        SetLogger(logger);

        // Ensure that the module constructor of each storm assembly is run
        LoadReferencedAssemblies();

        configuration(new StormConfiguration());

        StormControllerCache.FinishInitialization();

        IsInitialized = true;
    }

    /// <summary>
    /// Loads referenced assemblies by iterating through all assemblies in the current AppDomain, checking for <see cref="StormAssemblyAttribute"/> and loading referenced assemblies recursively.
    /// </summary>
    private static void LoadReferencedAssemblies()
    {
        var loadedAssemblies = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Where(a => IsSystemAssembly(a.FullName)).Select(a => a.FullName!));
        var assembliesToCheck = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

        while (assembliesToCheck.Count > 0)
        {
            var assembly = assembliesToCheck.Dequeue();

            if (assembly.GetCustomAttribute<StormAssemblyAttribute>() is not null)
            {
                // Ensure that the module constructor of the assembly is run
                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
            }

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (loadedAssemblies.Contains(reference.FullName) || IsSystemAssembly(reference.FullName))
                    continue;

                var loadedAssembly = Assembly.Load(reference);
                assembliesToCheck.Enqueue(loadedAssembly);
                loadedAssemblies.Add(reference.FullName);
            }
        }
    }

    /// <summary>
    /// Checks if the provided assembly full name belongs to a system assembly.
    /// </summary>
    /// <param name="assemblyFullName">The full name of the assembly to check.</param>
    /// <returns>
    /// True if the assembly full name is null, starts with "System.", or starts with "Microsoft."; otherwise, false.
    /// </returns>
    private static bool IsSystemAssembly(string? assemblyFullName)
    {
        return assemblyFullName?.StartsWith("System.") != false || assemblyFullName.StartsWith("Microsoft.");
    }

    /// <summary>
    /// Sets the logger for the ORM.
    /// </summary>
    /// <param name="logger">The logger to set.</param>
    public static void SetLogger(ILogger? logger)
    {
        Logger = logger;
        IsTraceEnabled = logger?.IsEnabled(LogLevel.Trace) == true;
    }
}

/// <summary>
/// Delegate for creating a database command.
/// </summary>
/// <param name="haveInputOutputParams">A boolean value indicating whether the command will have input/output parameters.</param>
/// <returns>The created database command.</returns>
public delegate StormDbCommand CreateCommandDelegate(bool haveInputOutputParams);

/// <summary>
/// Delegate for creating a database batch command.
/// </summary>
/// <param name="haveInputOutputParams">A boolean value indicating whether the command will have input/output parameters.</param>
/// <returns>The created database command.</returns>
public delegate StormDbBatchCommand CreateBatchCommandDelegate(bool haveInputOutputParams);

/// <summary>
/// Delegate for converting a UnifiedDbType to a string representation of a SqlDbType.
/// </summary>
public delegate string ToSqlDbTypeDelegate(UnifiedDbType dbType, int size, int precision, int scale);

/// <summary>
/// Delegate for adding a database parameter to a database command.
/// </summary>
/// <param name="command">The DbCommand to add the parameter to.</param>
/// <param name="parameterName">The name of the parameter.</param>
/// <param name="dbType">The database type of the parameter.</param>
/// <param name="size">The size of the parameter.</param>
/// <param name="value">The value of the parameter.</param>
/// <param name="direction">The direction of the parameter.</param>
/// <returns>The added database parameter.</returns>
public delegate StormDbParameter AddDbParameterDelegate(StormDbCommand command, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction = ParameterDirection.Input);

/// <summary>
/// Delegate for adding a database batch parameter to a DbBatchCommand.
/// </summary>
/// <param name="command">The DbBatchCommand to add the parameter to.</param>
/// <param name="parameterName">The name of the parameter.</param>
/// <param name="dbType">The database type of the parameter.</param>
/// <param name="size">The size of the parameter.</param>
/// <param name="value">The value of the parameter.</param>
/// <param name="direction">The direction of the parameter.</param>
/// <returns>The added database parameter.</returns>
public delegate StormDbParameter AddDbBatchParameterDelegate(StormDbBatchCommand command, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction = ParameterDirection.Input);

/// <summary>
/// Represents a delegate that converts an object to a string.
/// </summary>
public delegate string ToTextDelegate(object value, Type? typeToSerialize);

/// <summary>
/// Represents a delegate that converts a text value to an object of a specified type.
/// </summary>
public delegate object FromTextDelegate(string text, Type returnType);

/// <summary>
/// Delegate that represents a method which handles a DbException and returns an Exception.
/// </summary>
public delegate Exception? HandleDbExceptionDelegate(StormDbException dbException);
