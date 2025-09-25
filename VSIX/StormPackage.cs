using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.ToolWindows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace AltaSoft.Storm
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideToolWindow(typeof(StormToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.MainWindow, Transient = true, MultiInstances = true)]
    //[ProvideToolWindowVisibility(typeof(StormToolWindow.Pane), VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidAltaSoftStormPackageString)]
    public sealed class StormPackage : ToolkitPackage
    {
        internal static StormPackage s_packageInstance = default!;
        internal static EnvDTE80.DTE2 s_dte2Instance = default!;

        public StormPackage()
        {
            s_packageInstance = this;
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            s_dte2Instance = await this.GetServiceAsync<EnvDTE.DTE, EnvDTE80.DTE2>();

            await this.RegisterCommandsAsync();

            this.RegisterToolWindows();
        }
    }
}
