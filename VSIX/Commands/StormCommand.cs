using System.Threading.Tasks;
using AltaSoft.Storm.ToolWindows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace AltaSoft.Storm.Commands;

[Command(PackageIds.StormCommand)]
internal sealed class StormCommand : BaseCommand<StormCommand>
{
    protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        return StormToolWindow.ShowAsync();
    }
}
