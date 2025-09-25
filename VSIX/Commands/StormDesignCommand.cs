using System;
using System.Threading.Tasks;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.ToolWindows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace AltaSoft.Storm.Commands;

[Command(PackageIds.StormDesignCommand)]
public class StormDesignCommand : BaseCommand<StormDesignCommand>
{
    protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        return StormToolWindow.ShowAsync();
    }

    protected override void BeforeQueryStatus(EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var activeProject = ProjectHelpers.GetActiveProject();

        Command.Visible = activeProject?.HasAltaSoftStormReference() == true;

        base.BeforeQueryStatus(e);
    }
}
