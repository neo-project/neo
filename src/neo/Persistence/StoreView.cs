using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Collections.Generic;

namespace Neo.Persistence
{
    /// <summary>
    /// It provides a set of properties and methods for reading formatted data from the underlying storage. Such as <see cref="Blocks"/> and <see cref="Transactions"/>.
    /// </summary>
    public abstract class StoreView
    {
        public Block PersistingBlock { get; internal set; }
        public abstract DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public abstract DataCache<UInt256, TransactionState> Transactions { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }
        public abstract DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList { get; }
        public abstract MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public abstract MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public uint Height => BlockHashIndex.Get().Index;
        public uint HeaderHeight => HeaderHashIndex.Get().Index;
        public UInt256 CurrentBlockHash => BlockHashIndex.Get().Hash;
        public UInt256 CurrentHeaderHash => HeaderHashIndex.Get().Hash;
        public Dictionary<UInt160, ContractState> ContractSet;

        public StoreView Clone()
        {
            return new ClonedView(this);
        }

        public virtual void Commit()
        {
            Blocks.Commit();
            Transactions.Commit();
            Storages.Commit();
            HeaderHashList.Commit();
            BlockHashIndex.Commit();
            HeaderHashIndex.Commit();
        }

        public bool ContainsBlock(UInt256 hash)
        {
            TrimmedBlock state = Blocks.TryGet(hash);
            if (state == null) return false;
            return state.IsBlock;
        }

        public bool ContainsTransaction(UInt256 hash)
        {
            return Transactions.Contains(hash);
        }

        public Block GetBlock(UInt256 hash)
        {
            TrimmedBlock state = Blocks.TryGet(hash);
            if (state == null) return null;
            if (!state.IsBlock) return null;
            return state.GetBlock(Transactions);
        }

        public Header GetHeader(UInt256 hash)
        {
            return Blocks.TryGet(hash)?.Header;
        }

        public Transaction GetTransaction(UInt256 hash)
        {
            return Transactions.TryGet(hash)?.Transaction;
        }
    }
}
