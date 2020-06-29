using System;
using Neo.Ledger;

namespace Neo.Persistence
{
    internal static class Helper
    {
        public static byte[] EnsureNotNull(this byte[] source)
        {
            return source ?? Array.Empty<byte>();
        }

        public static void UpdateLocalStateRoot(this SnapshotView snapshot)
        {
            snapshot.Storages.Commit();
            var root = snapshot.LocalStateRoot.GetAndChange(snapshot.Height, () => new HashIndexState());
            root.Index = snapshot.Height;
            root.Hash = ((MPTDataCache<StorageKey, StorageItem>)snapshot.Storages).Root.Hash;
        }
    }
}
