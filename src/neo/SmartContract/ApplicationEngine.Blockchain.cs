using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const uint MaxTraceableBlocks = 2_102_400; // 365 days

        public static readonly InteropDescriptor System_Blockchain_GetHeight = Register("System.Blockchain.GetHeight", nameof(GetBlockchainHeight), 0_00000400, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Blockchain_GetBlock = Register("System.Blockchain.GetBlock", nameof(GetBlock), 0_02500000, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Blockchain_GetTransaction = Register("System.Blockchain.GetTransaction", nameof(GetTransaction), 0_01000000, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Blockchain_GetTransactionHeight = Register("System.Blockchain.GetTransactionHeight", nameof(GetTransactionHeight), 0_01000000, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Blockchain_GetTransactionFromBlock = Register("System.Blockchain.GetTransactionFromBlock", nameof(GetTransactionFromBlock), 0_01000000, CallFlags.AllowStates, true);
        public static readonly InteropDescriptor System_Blockchain_GetContract = Register("System.Blockchain.GetContract", nameof(GetContract), 0_01000000, CallFlags.AllowStates, true);

        protected internal uint GetBlockchainHeight()
        {
            return Snapshot.Height;
        }

        protected internal Block GetBlock(byte[] indexOrHash)
        {
            UInt256 hash;
            if (indexOrHash.Length < UInt256.Length)
            {
                BigInteger bi = new BigInteger(indexOrHash);
                if (bi < uint.MinValue || bi > uint.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(indexOrHash));
                hash = Blockchain.Singleton.GetBlockHash((uint)bi);
            }
            else if (indexOrHash.Length == UInt256.Length)
            {
                hash = new UInt256(indexOrHash);
            }
            else
            {
                throw new ArgumentException();
            }
            if (hash is null) return null;
            Block block = Snapshot.GetBlock(hash);
            if (block is null) return null;
            if (!IsTraceableBlock(Snapshot, block.Index)) return null;
            return block;
        }

        protected internal Transaction GetTransaction(UInt256 hash)
        {
            TransactionState state = Snapshot.Transactions.TryGet(hash);
            if (state != null && !IsTraceableBlock(Snapshot, state.BlockIndex)) state = null;
            return state?.Transaction;
        }

        protected internal int GetTransactionHeight(UInt256 hash)
        {
            TransactionState state = Snapshot.Transactions.TryGet(hash);
            if (state is null) return -1;
            if (!IsTraceableBlock(Snapshot, state.BlockIndex)) return -1;
            return (int)state.BlockIndex;
        }

        protected internal Transaction GetTransactionFromBlock(byte[] blockIndexOrHash, int txIndex)
        {
            UInt256 hash;
            if (blockIndexOrHash.Length < UInt256.Length)
            {
                BigInteger bi = new BigInteger(blockIndexOrHash);
                if (bi < uint.MinValue || bi > uint.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(blockIndexOrHash));
                hash = Blockchain.Singleton.GetBlockHash((uint)bi);
            }
            else if (blockIndexOrHash.Length == UInt256.Length)
            {
                hash = new UInt256(blockIndexOrHash);
            }
            else
            {
                throw new ArgumentException();
            }
            if (hash is null) return null;
            TrimmedBlock block = Snapshot.Blocks.TryGet(hash);
            if (block is null) return null;
            if (!IsTraceableBlock(Snapshot, block.Index)) return null;
            if (txIndex < 0 || txIndex >= block.Hashes.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(txIndex));
            return Snapshot.GetTransaction(block.Hashes[txIndex + 1]);
        }

        protected internal ContractState GetContract(UInt160 hash)
        {
            return Snapshot.Contracts.TryGet(hash);
        }

        private static bool IsTraceableBlock(StoreView snapshot, uint index)
        {
            if (index > snapshot.Height) return false;
            return index + MaxTraceableBlocks > snapshot.Height;
        }
    }
}
