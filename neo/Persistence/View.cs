using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.Persistence
{
    public abstract class View
    {
        public Block PersistingBlock { get; internal set; }
        public abstract DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public abstract DataCache<UInt256, TransactionState> Transactions { get; }
        public abstract DataCache<UInt160, ContractState> Contracts { get; }
        public abstract DataCache<StorageKey, StorageItem> Storages { get; }
        public abstract DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public abstract MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public abstract MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public uint Height => BlockHashIndex.Get().Index;
        public uint HeaderHeight => HeaderHashIndex.Get().Index;
        public UInt256 CurrentBlockHash => BlockHashIndex.Get().Hash;
        public UInt256 CurrentHeaderHash => HeaderHashIndex.Get().Hash;

        public View Clone()
        {
            return new ClonedView(this);
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

        public bool ContainsBlock(UInt256 hash)
        {
            TrimmedBlock state = Blocks.TryGet(hash);
            if (state == null) return false;
            return state.IsBlock;
        }

        public bool ContainsTransaction(UInt256 hash)
        {
            TransactionState state = Transactions.TryGet(hash);
            return state != null;
        }

        public Block GetBlock(uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock;
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return GetBlock(hash);
        }

        public Block GetBlock(UInt256 hash)
        {
            TrimmedBlock state = Blocks.TryGet(hash);
            if (state == null) return null;
            if (!state.IsBlock) return null;
            return state.GetBlock(Transactions);
        }

        public Header GetHeader(uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock.Header;
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return GetHeader(hash);
        }

        public Header GetHeader(UInt256 hash)
        {
            return Blocks.TryGet(hash)?.Header;
        }

        public UInt256 GetNextBlockHash(UInt256 hash)
        {
            TrimmedBlock state = Blocks.TryGet(hash);
            if (state == null) return null;
            return Blockchain.Singleton.GetBlockHash(state.Index + 1);
        }

        public Transaction GetTransaction(UInt256 hash)
        {
            return Transactions.TryGet(hash)?.Transaction;
        }
    }
}
