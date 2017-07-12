using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Neo.IO.Caching
{
    internal class TrackableCollection<TKey, TItem> : KeyedCollection<TKey, TItem> where TItem : ITrackable<TKey>
    {
        public TrackableCollection() { }

        public TrackableCollection(IEnumerable<TItem> items)
        {
            foreach (TItem item in items)
            {
                base.InsertItem(Count, item);
                item.TrackState = TrackState.None;
            }
        }

        protected override void ClearItems()
        {
            for (int i = Count - 1; i >= 0; i--)
                RemoveItem(i);
        }

        public void Commit()
        {
            for (int i = Count - 1; i >= 0; i--)
                if (Items[i].TrackState == TrackState.Deleted)
                    base.RemoveItem(i);
                else
                    Items[i].TrackState = TrackState.None;
        }

        public TItem[] GetChangeSet()
        {
            return Items.Where(p => p.TrackState != TrackState.None).ToArray();
        }

        protected override TKey GetKeyForItem(TItem item)
        {
            return item.Key;
        }

        protected override void InsertItem(int index, TItem item)
        {
            base.InsertItem(index, item);
            item.TrackState = TrackState.Added;
        }

        protected override void RemoveItem(int index)
        {
            if (Items[index].TrackState == TrackState.Added)
                base.RemoveItem(index);
            else
                Items[index].TrackState = TrackState.Deleted;
        }
    }
}
