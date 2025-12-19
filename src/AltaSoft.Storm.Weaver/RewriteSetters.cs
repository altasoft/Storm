using Microsoft.Build.Framework;

namespace AltaSoft.Storm.Weaver;

/// <summary>
/// Represents a task that rewrites property setters in an assembly using IL weaving.
/// </summary>
/// <returns>
/// True if the task executed successfully; otherwise, false.
/// </returns>
public sealed class RewriteSettersTask : Microsoft.Build.Utilities.Task, ICancelableTask
{
    [Required]
    public string TargetAssembly { get; set; } = default!;

    [Required]
    public string TargetFramework { get; set; } = default!;

    [Required]
    public string TargetDir { get; set; } = default!;

    [Required]
    public string References { get; set; } = default!;

    /// <inheritdoc/>
    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Starting IL weaving for '{TargetAssembly}' ({TargetFramework})");

        Log.LogMessage(MessageImportance.High, $"$(TargetAssembly) = '{TargetAssembly}'");
        Log.LogMessage(MessageImportance.High, $"$(TargetFramework) = '{TargetFramework}'");
        Log.LogMessage(MessageImportance.High, $"$(TargetDir) = '{TargetDir}'");
        Log.LogMessage(MessageImportance.High, $"$(ReferencePath) = '{References}'");

        ILWeaver.InterceptPropertySetters(TargetAssembly, TargetFramework, TargetDir, Log, References.Split([';'], StringSplitOptions.RemoveEmptyEntries));

        Log.LogMessage(MessageImportance.High, $"AltaSoft.Storm: IL weaving complete for '{TargetAssembly}' ({TargetFramework})");

        // Log.HasLoggedErrors is true if the task logged any errors -- even if they were logged
        // from a task's constructor or property setter. As long as this task is written to always log an error when it fails, we can reliably return HasLoggedErrors.
        return !Log.HasLoggedErrors;
    }

    public void Cancel() => ILWeaver.Cancel();
}
