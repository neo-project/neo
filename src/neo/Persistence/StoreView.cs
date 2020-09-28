using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;

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
        public abstract DataCache<UInt160, ContractState> Contracts { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }
        public abstract DataCache<UInt160, ByteArraySerializable> CachedScripts { get; }
        public abstract DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList { get; }
        public abstract MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public abstract MetaDataCache<HashIndexState> HeaderHashIndex { get; }
        public abstract MetaDataCache<ContractIdState> ContractId { get; }

        public uint Height => BlockHashIndex.Get().Index;
        public uint HeaderHeight => HeaderHashIndex.Get().Index;
        public UInt256 CurrentBlockHash => BlockHashIndex.Get().Hash;
        public UInt256 CurrentHeaderHash => HeaderHashIndex.Get().Hash;

        public StoreView Clone()
        {
            return new ClonedView(this);
        }

        public virtual void Commit()
        {
            Blocks.Commit();
            Transactions.Commit();
            Contracts.Commit();
            Storages.Commit();
            CachedScripts.Commit();
            HeaderHashList.Commit();
            BlockHashIndex.Commit();
            HeaderHashIndex.Commit();
            ContractId.Commit();
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
            if (state.Witness.VerificationScript.Length == 0)
            {
                state.Witness.VerificationScript = CachedScripts.TryGet(state.GetScriptHashesForVerifying(this)[0]);
            }
            return state.GetBlock(Transactions);
        }

        public Header GetHeader(UInt256 hash)
        {
            TrimmedBlock state = Blocks.TryGet(hash);
            if (state == null) return null;
            if (state.Witness.VerificationScript.Length == 0)
            {
                state.Witness.VerificationScript = CachedScripts.TryGet(state.GetScriptHashesForVerifying(this)[0]);
            }
            return state.Header;
        }

        public Transaction GetTransaction(UInt256 hash)
        {
            return Transactions.TryGet(hash)?.Transaction;
        }
    }
}
