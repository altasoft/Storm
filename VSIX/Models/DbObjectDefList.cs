using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace AltaSoft.Storm.Models;

internal class DbObjectDefList : ObservableCollection<DbObjectDef>
{
    public void Refresh()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
    }
}
