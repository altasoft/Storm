using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AltaSoft.Storm.Models;

internal class StormTypeDefList : ObservableCollection<StormTypeDef>
{
    public void Refresh()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
