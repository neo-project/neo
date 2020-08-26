
using System.Collections.Generic;

namespace Neo.Trie.MPT
{
    public enum TrackState : byte
    {
        None,
        Put,
        Deleted
    }

    public class Trackable
    {
        public byte[] RawNode;
        public TrackState State;
    }

    public class MPTDb
    {
        private IKVStore store;
        private Dictionary<UInt256, Trackable> cache = new Dictionary<UInt256, Trackable>();

        public MPTDb(IKVStore store)
        {
            this.store = store;
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            if (cache.TryGetValue(hash, out Trackable t))
            {
                if (t.State == TrackState.Deleted) return null;
                return MPTNode.DeserializeFromByteArray(t.RawNode);
            }
            var data = store.Get(hash.ToArray());
            cache.Add(hash, new Trackable
            {
                RawNode = data,
                State = TrackState.None,
            });
            return MPTNode.DeserializeFromByteArray(data);
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode hn)
            {
                throw new System.InvalidOperationException("Means nothing to store HashNode");
            }
            if (cache.TryGetValue(node.GetHash(), out Trackable t))
            {
                t.RawNode = node.ToArrayWithReferences();
                t.State = TrackState.Put;
                return;
            }
            cache.Add(node.GetHash(), new Trackable
            {
                RawNode = node.ToArrayWithReferences(),
                State = TrackState.Put,
            });
        }

        public void Delete(UInt256 hash)
        {
            if (cache.TryGetValue(hash, out Trackable t))
            {
                t.State = TrackState.Deleted;
                return;
            }
            cache.Add(hash, new Trackable
            {
                RawNode = null,
                State = TrackState.Deleted,
            });
        }

        public void Commit()
        {
            foreach (var item in cache)
            {
                switch (item.Value.State)
                {
                    case TrackState.Put:
                        store.Put(item.Key.ToArray(), item.Value.RawNode);
                        break;
                    case TrackState.Deleted:
                        store.Delete(item.Key.ToArray());
                        break;
                }
            }
        }
    }
}
