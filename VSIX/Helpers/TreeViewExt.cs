using System.Windows.Controls;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Extension class for TreeView control
/// </summary>
internal static class TreeViewExt
{
    /// <summary>
    /// Extension method for selecting an item in a TreeView.
    /// </summary>
    /// <param name="treeView">The TreeView control.</param>
    /// <param name="item">The item to be selected.</param>
    public static void SelectItem(this TreeView treeView, object item)
    {
        if (treeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvItem)
        {
            tvItem.IsSelected = true;
        }
    }

    /// <summary>
    /// Selects the first item in a TreeView.
    /// </summary>
    /// <param name="treeView">The TreeView to select the item from.</param>
    /// <returns>True if an item was selected, false otherwise.</returns>
    public static bool TrySelectFirstItem(this TreeView treeView)
    {
        if (treeView.Items.Count > 0)
        {
            treeView.SelectItem(treeView.Items[0]);
            return true;
        }
        return false;
    }
}
