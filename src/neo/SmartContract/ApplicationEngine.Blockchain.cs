using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public const uint MaxTraceableBlocks = Transaction.MaxValidUntilBlockIncrement;

        [InteropService("System.Blockchain.GetHeight", 0_00000400, TriggerType.Application, CallFlags.AllowStates)]
        private bool Blockchain_GetHeight()
        {
            Push(Snapshot.Height);
            return true;
        }

        [InteropService("System.Blockchain.GetBlock", 0_02500000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Blockchain_GetBlock()
        {
            UInt256 hash;
            if (TryPop(out uint height))
            {
                hash = Ledger.Blockchain.Singleton.GetBlockHash(height);
            }
            else if (TryPop(out ReadOnlySpan<byte> data))
            {
                if (data.Length != 32) return false;
                hash = new UInt256(data);
            }
            else
            {
                return false;
            }
            Block block = hash != null ? Snapshot.GetBlock(hash) : null;
            if (block != null && !IsTraceableBlock(block.Index)) block = null;
            Push(block?.ToStackItem(ReferenceCounter) ?? StackItem.Null);
            return true;
        }

        [InteropService("System.Blockchain.GetTransaction", 0_01000000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Blockchain_GetTransaction()
        {
            if (!TryPop(out ReadOnlySpan<byte> hash)) return false;
            TransactionState state = Snapshot.Transactions.TryGet(new UInt256(hash));
            if (state != null && !IsTraceableBlock(state.BlockIndex)) state = null;
            Push(state?.Transaction.ToStackItem(ReferenceCounter) ?? StackItem.Null);
            return true;
        }

        [InteropService("System.Blockchain.GetTransactionHeight", 0_01000000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Blockchain_GetTransactionHeight()
        {
            if (!TryPop(out ReadOnlySpan<byte> hash)) return false;
            TransactionState state = Snapshot.Transactions.TryGet(new UInt256(hash));
            if (state != null && !IsTraceableBlock(state.BlockIndex)) state = null;
            Push(state?.BlockIndex ?? BigInteger.MinusOne);
            return true;
        }

        [InteropService("System.Blockchain.GetTransactionFromBlock", 0_01000000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Blockchain_GetTransactionFromBlock()
        {
            UInt256 hash;
            if (TryPop(out uint height))
            {
                hash = Ledger.Blockchain.Singleton.GetBlockHash(height);
            }
            else if (TryPop(out ReadOnlySpan<byte> data))
            {
                if (data.Length != 32) return false;
                hash = new UInt256(data);
            }
            else
            {
                return false;
            }
            TrimmedBlock block = hash != null ? Snapshot.Blocks.TryGet(hash) : null;
            if (block != null && !IsTraceableBlock(block.Index)) block = null;
            if (block is null)
            {
                Push(StackItem.Null);
            }
            else
            {
                if (!TryPop(out int index)) return false;
                if (index < 0 || index >= block.Hashes.Length - 1) return false;
                Transaction tx = Snapshot.GetTransaction(block.Hashes[index + 1]);
                Push(tx?.ToStackItem(ReferenceCounter) ?? StackItem.Null);
            }
            return true;
        }

        [InteropService("System.Blockchain.GetContract", 0_01000000, TriggerType.Application, CallFlags.AllowStates)]
        private bool Blockchain_GetContract()
        {
            UInt160 hash = new UInt160(Pop().GetSpan());
            ContractState contract = Snapshot.Contracts.TryGet(hash);
            if (contract == null)
                Push(StackItem.Null);
            else
                Push(contract.ToStackItem(ReferenceCounter));
            return true;
        }

        private bool IsTraceableBlock(uint index)
        {
            if (index > Snapshot.Height) return false;
            return index + MaxTraceableBlocks > Snapshot.Height;
        }
    }
}
