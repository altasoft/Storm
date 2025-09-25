using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace AltaSoft.Storm.Weaver;

public sealed class CustomAssemblyResolver : IAssemblyResolver
{
    private readonly Dictionary<string, string> _referenceDictionary;
    private readonly Dictionary<string, AssemblyDefinition> _assemblyDefinitionCache = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly TaskLoggingHelper _logger;

    public CustomAssemblyResolver(string targetAssembly, string targetFramework, string targetDir, TaskLoggingHelper logger, IEnumerable<string> splitReferences)
    {
        _referenceDictionary = new Dictionary<string, string>();
        _logger = logger;

        foreach (var filePath in splitReferences)
        {
            _referenceDictionary[GetAssemblyName(filePath)] = filePath;
        }
    }

    private string GetAssemblyName(string filePath)
    {
        try
        {
            return GetAssembly(filePath, new ReaderParameters(ReadingMode.Deferred)).Name.Name;
        }
        catch (Exception ex)
        {
            _logger.LogMessage(MessageImportance.High, $"Could not load {filePath}, assuming the assembly name is equal to the file name: {ex}");
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }

    private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        if (_assemblyDefinitionCache.TryGetValue(file, out var assembly))
        {
            return assembly;
        }

        parameters.AssemblyResolver ??= this;
        try
        {
            return _assemblyDefinitionCache[file] = AssemblyDefinition.ReadAssembly(file, parameters);
        }
        catch (Exception exception)
        {
            throw new Exception($"Could not read '{file}'.", exception);
        }
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference assemblyNameReference)
    {
        return Resolve(assemblyNameReference, new ReaderParameters());
    }

    public AssemblyDefinition? Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters? parameters)
    {
        parameters ??= new ReaderParameters();

        return _referenceDictionary.TryGetValue(assemblyNameReference.Name, out var fileFromDerivedReferences)
            ? GetAssembly(fileFromDerivedReferences, parameters)
            : null;
    }

    public void Dispose()
    {
        foreach (var value in _assemblyDefinitionCache.Values)
        {
            value?.Dispose();
        }
    }
}

