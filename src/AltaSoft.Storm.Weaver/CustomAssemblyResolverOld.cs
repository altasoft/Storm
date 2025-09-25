//using Microsoft.Build.Framework;
//using Microsoft.Build.Utilities;
//using Mono.Cecil;
//using NuGet.Frameworks;

//namespace AltaSoft.Storm.Weaver;

///// <summary>
///// CustomAssemblyResolver is a class that extends the BaseAssemblyResolver class and is used to resolve assembly references.
///// It maintains a dictionary of resolved assemblies and uses a DefaultAssemblyResolver to resolve assemblies.
///// If an assembly is not found, it tries to resolve it from the NuGet package and adds it to the dictionary.
///// It also provides a method to find an assembly in the NuGet packages.
///// </summary>
//internal sealed class CustomAssemblyResolver : DefaultAssemblyResolver
//{
//    private readonly TaskLoggingHelper _logger;

//    public CustomAssemblyResolver(string targetAssembly, string targetFramework, string targetDir, TaskLoggingHelper logger)
//    {
//        _logger = logger;

//        AddSearchDirectory(targetDir);
//        logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Base location added: '{targetDir}'");

//        var location = Path.GetDirectoryName(typeof(Type).Assembly.Location);
//        AddSearchDirectory(location);
//        logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: System.Type Location added: '{location}'");

//        location = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("file:///", ""));
//        if (string.IsNullOrEmpty(location))
//            return;

//        AddSearchDirectory(location);
//        logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Location added: '{location}'");

//        location = Path.Combine(location, "..", "..", "lib");
//        if (!Directory.Exists(location))
//            return;

//        var availableFrameworks = Directory.GetDirectories(location);
//        if (availableFrameworks.Length == 0)
//            return;

//        var framework = ResolveNearestFramework(targetFramework, availableFrameworks.Select(Path.GetFileName));
//        if (framework is null)
//            return;

//        location = Path.Combine(location, framework);

//        AddSearchDirectory(location);
//        logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Location added: '{location}'");
//    }

//    public override AssemblyDefinition? Resolve(AssemblyNameReference name)
//    {
//        _logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Resolving assembly: {name}");
//        try
//        {
//            return base.Resolve(name);
//        }
//        catch (AssemblyResolutionException ex)
//        {
//            _logger.LogMessage(MessageImportance.High, $"AltaSoft.Storm: Assembly not found: {name}.\n{ex}");

//            var basePath = Directory.GetCurrentDirectory();
//            _logger.LogMessage($"Base path: '{basePath}'");
//            //string fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
//            throw;
//        }
//    }

//    protected override AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
//    {
//        _logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Searching directory: {name}");
//        var dirs = directories.ToArray();
//        foreach (var directory in dirs)
//        {
//            _logger.LogMessage(MessageImportance.Low, $"  -- {directory}");
//        }

//        return base.SearchDirectory(name, dirs, parameters);
//    }

//    private static string? ResolveNearestFramework(string targetFramework, IEnumerable<string> availableFrameworks)
//    {
//        var targetNuGetFramework = NuGetFramework.ParseFolder(targetFramework);
//        var availableNuGetFrameworks = availableFrameworks.Select(NuGetFramework.ParseFolder).ToList();

//        var reducer = new FrameworkReducer();
//        var nearestFramework = reducer.GetNearest(targetNuGetFramework, availableNuGetFrameworks);

//        return nearestFramework?.GetShortFolderName();
//    }
//}
