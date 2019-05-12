using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.Persistence
{
    public static class Helper
    {
        public static bool ContainsBlock(this IPersistence persistence, UInt256 hash)
        {
            BlockState state = persistence.Blocks.TryGet(hash);
            if (state == null) return false;
            return state.TrimmedBlock.IsBlock;
        }

        public static bool ContainsTransaction(this IPersistence persistence, UInt256 hash)
        {
            TransactionState state = persistence.Transactions.TryGet(hash);
            return state != null;
        }

        public static Block GetBlock(this IPersistence persistence, uint index)
        {
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return persistence.GetBlock(hash);
        }

        public static Block GetBlock(this IPersistence persistence, UInt256 hash)
        {
            BlockState state = persistence.Blocks.TryGet(hash);
            if (state == null) return null;
            if (!state.TrimmedBlock.IsBlock) return null;
            return state.TrimmedBlock.GetBlock(persistence.Transactions);
        }

        public static Header GetHeader(this IPersistence persistence, uint index)
        {
            UInt256 hash = Blockchain.Singleton.GetBlockHash(index);
            if (hash == null) return null;
            return persistence.GetHeader(hash);
        }

        public static Header GetHeader(this IPersistence persistence, UInt256 hash)
        {
            return persistence.Blocks.TryGet(hash)?.TrimmedBlock.Header;
        }

        public static UInt256 GetNextBlockHash(this IPersistence persistence, UInt256 hash)
        {
            BlockState state = persistence.Blocks.TryGet(hash);
            if (state == null) return null;
            return Blockchain.Singleton.GetBlockHash(state.TrimmedBlock.Index + 1);
        }

        public static long GetSysFeeAmount(this IPersistence persistence, uint height)
        {
            var hash = Blockchain.Singleton.GetBlockHash(height);
            return hash == null ? 0 : persistence.GetSysFeeAmount(hash);
        }

        public static long GetSysFeeAmount(this IPersistence persistence, UInt256 hash)
        {
            BlockState block_state = persistence.Blocks.TryGet(hash);
            if (block_state == null) return 0;
            return block_state.SystemFeeAmount;
        }

        public static Transaction GetTransaction(this IPersistence persistence, UInt256 hash)
        {
            return persistence.Transactions.TryGet(hash)?.Transaction;
        }
    }
}
