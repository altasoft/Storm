using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;

using DteProject = EnvDTE.Project;

namespace AltaSoft.Storm.Helpers;

internal static class ProjectHelpers
{
    private const string StormNugetPackageName = "AltaSoft.Storm.Generator.MsSql";

    public static DteProject? GetActiveProject()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        return StormPackage.s_dte2Instance.ActiveSolutionProjects is Array { Length: > 0 } activeSolutionProjects
            ? activeSolutionProjects.GetValue(0) as DteProject
            : null;
    }

    /// <summary>
    /// Checks if the given project has a reference to the AltaSoft.Storm library.
    /// </summary>
    /// <param name="project">The project to check.</param>
    /// <returns>True if the project has a reference to the AltaSoft.Storm library, false otherwise.</returns>
    public static bool HasAltaSoftStormReference(this DteProject project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var hasReference = (project.Object as VSLangProj.VSProject)?.References.Find(StormNugetPackageName) is not null;
        if (hasReference)
            return hasReference;

        if (project.FileName is null)
            return false;

        var xElement = XDocument.Load(project.FileName).Root;
        if (xElement is null)
            return false;

        return xElement.DescendantNodes().OfType<XElement>()
            .Any(x => x.Name.LocalName.Equals("PackageReference") && x.Attribute("Include")?.Value.Equals(StormNugetPackageName) == true);
    }
}
