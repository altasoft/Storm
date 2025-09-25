using System;
using System.IO;
using System.Threading.Tasks;
using AltaSoft.Storm.Models;
using Community.VisualStudio.Toolkit;

namespace AltaSoft.Storm.Helpers;

internal static class ConnectionDataExt
{
    /// <summary>
    /// Returns the file path for the .storm file associated with the given project file name.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns>The file path for the .storm file.</returns>
    private static string GetStormFilePath(Project project)
    {
        var fileName = Path.GetFileName(project.FullPath);
        var folder = Path.GetDirectoryName(project.FullPath) ?? Path.DirectorySeparatorChar.ToString();

        return Path.Combine(folder, "Properties", Path.ChangeExtension(fileName, ".storm"));
    }

    public static async Task SaveConnectionDataAsync(this ConnectionData connectionData)
    {
        var project = await VS.Solutions.GetActiveProjectAsync();
        if (project?.IsLoaded != true || project.FullPath is null)
            return;

        var filePath = GetStormFilePath(project);

        var json = System.Text.Json.JsonSerializer.Serialize(connectionData);

        var folder = Path.GetDirectoryName(filePath);
        if (folder is not null && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(filePath, json);
        await project.AddExistingFilesAsync(filePath);
    }

    public static async Task<ConnectionData?> ReadConnectionDataAsync()
    {
        var project = await VS.Solutions.GetActiveProjectAsync();
        if (project?.IsLoaded != true || project.FullPath is null)
            return null;

        var configFile = GetStormFilePath(project);
        if (!File.Exists(configFile))
            return null;

        var json = File.ReadAllText(configFile);
        var result = System.Text.Json.JsonSerializer.Deserialize<ConnectionData>(json);

        return result is null || result.Provider == Guid.Empty || result.Source == Guid.Empty || string.IsNullOrEmpty(result.ConnectionString)
            ? null
            : result;
    }
}
