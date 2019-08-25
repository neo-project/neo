using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.Persistence
{
    public static class Helper
    {
        public static bool ContainsBlock(this IPersistence persistence, UInt256 hash)
        {
            TrimmedBlock state = persistence.Blocks.TryGet(hash);
            if (state == null) return false;
            return state.IsBlock;
        }

        public static bool ContainsTransaction(this IPersistence persistence, UInt256 hash)
        {
            TransactionState state = persistence.Transactions.TryGet(hash);
            return state != null;
        }

        public static Block GetBlock(this IPersistence persistence, uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock;
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return persistence.GetBlock(hash);
        }

        public static Block GetBlock(this IPersistence persistence, UInt256 hash)
        {
            TrimmedBlock state = persistence.Blocks.TryGet(hash);
            if (state == null) return null;
            if (!state.IsBlock) return null;
            return state.GetBlock(persistence.Transactions);
        }

        public static Header GetHeader(this IPersistence persistence, uint index)
        {
            if (index == 0) return Blockchain.GenesisBlock.Header;
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return persistence.GetHeader(hash);
        }

        public static Header GetHeader(this IPersistence persistence, UInt256 hash)
        {
            return persistence.Blocks.TryGet(hash)?.Header;
        }

        public static UInt256 GetNextBlockHash(this IPersistence persistence, UInt256 hash)
        {
            TrimmedBlock state = persistence.Blocks.TryGet(hash);
            if (state == null) return null;
            return Blockchain.Singleton.GetBlockHash(state.Index + 1);
        }

        public static Transaction GetTransaction(this IPersistence persistence, UInt256 hash)
        {
            return persistence.Transactions.TryGet(hash)?.Transaction;
        }
    }
}
