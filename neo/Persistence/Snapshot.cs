using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace Neo.Persistence
{
    public abstract class Snapshot : IDisposable, IPersistence
    {
        public Block PersistingBlock { get; internal set; }
        public abstract DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public abstract DataCache<UInt256, TransactionState> Transactions { get; }
        public abstract DataCache<UInt160, ContractState> Contracts { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }

        // these updates may have happened on mempool only, so these can affect balance values (for example, avoid re-using spent values)
        public Dictionary<StorageKey, BigInteger> StorageUpdates = new Dictionary<StorageKey, BigInteger>();
        // gets a clone of storage item, or creates one (considering StorageUpdates cache)
        public StorageItem GetStorageFromCache(StorageKey key)
        {
            StorageItem sbase = Storages.TryGet(key);
            StorageItem sitem = new StorageItem(); // create new key for return
            sitem.Value = new byte[0];
            if(sbase != null)
            {
                sitem.Value = new byte[sbase.Value.Length];
                sitem.Value = sbase.Value.ToArray(); // TODO: clone other parts
            }
            
            if(StorageUpdates.TryGetValue(key, out BigInteger val))
            {
                var newval = new BigInteger(sitem.Value) + val;
                sitem.Value = newval.ToByteArray(); // TODO: update if 0 -> "", not "0x00"
            }
            return sitem;
        }

        public void AddToStorageCache(StorageKey key, BigInteger value)
        {
            if(StorageUpdates.TryGetValue(key, out BigInteger val))
            {
                StorageUpdates[key] += value;
            }
            else
                StorageUpdates[key] = value;
        }

        /*
        public void UpdateStorages()
        {
            // before Storage commit, cleanup dictionary cache
            foreach(KeyValuePair<StorageKey, BigInteger> entry in StorageUpdates)
            {
                StorageItem updateStore = GetStorage(entry.Key);
                StorageItem newitem = Storages.GetAndChange(entry.Key, () => new StorageItem());
                newitem.Value = updateStore.Value;
                StorageUpdates.Remove(entry.Key); // cleanup cached key
            }
        }
        */

        public abstract DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public abstract MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public abstract MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public uint Height => BlockHashIndex.Get().Index;
        public uint HeaderHeight => HeaderHashIndex.Get().Index;
        public UInt256 CurrentBlockHash => BlockHashIndex.Get().Hash;
        public UInt256 CurrentHeaderHash => HeaderHashIndex.Get().Hash;

        public Snapshot Clone()
        {
            return new CloneSnapshot(this);
        }

        public virtual void Commit()
        {
            Blocks.Commit();
            Transactions.Commit();
            Contracts.Commit();
            Storages.Commit();
            HeaderHashList.Commit();
            BlockHashIndex.Commit();
            HeaderHashIndex.Commit();
        }

        public virtual void Dispose()
        {
        }
    }
}
