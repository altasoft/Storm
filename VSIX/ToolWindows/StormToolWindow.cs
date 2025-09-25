using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AltaSoft.Storm.ToolWindows;

public sealed class StormToolWindow : BaseToolWindow<StormToolWindow>
{
    private ITrackSelection? _trackSelection;
    private SelectionContainer? _selContainer;

    public override string GetTitle(int toolWindowId) => $"AltaSoft.Storm Designer #{toolWindowId}";

    public override Type PaneType => typeof(Pane);

    public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
    {
        return Task.FromResult<FrameworkElement>(new StormToolWindowControl(SelectObject));
    }

    public override void SetPane(ToolWindowPane pane, int toolWindowId)
    {
        _trackSelection = pane.GetService<STrackSelection, ITrackSelection>();
        base.SetPane(pane, toolWindowId);
    }

    public void SelectObject(object selectedObject)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _selContainer = new SelectionContainer(true, false);
        var list = new ArrayList
        {
            selectedObject
        };

        _selContainer.SelectableObjects = list;
        _selContainer.SelectedObjects = list;

        _trackSelection?.OnSelectChange(_selContainer);
    }

    [Guid("0a02b82e-6254-4133-a480-ad539bda268c")]
    internal sealed class Pane : ToolkitToolWindowPane
    {
        public Pane()
        {
            BitmapImageMoniker = KnownMonikers.CompareDatabases;
        }
    }
}
