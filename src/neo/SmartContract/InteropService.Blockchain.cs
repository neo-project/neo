using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Blockchain
        {
            public const uint MaxTraceableBlocks = Transaction.MaxValidUntilBlockIncrement;

            public static readonly InteropDescriptor GetHeight = Register("System.Blockchain.GetHeight", Blockchain_GetHeight, 0_00000400, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetBlock = Register("System.Blockchain.GetBlock", Blockchain_GetBlock, 0_02500000, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetTransaction = Register("System.Blockchain.GetTransaction", Blockchain_GetTransaction, 0_01000000, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetTransactionHeight = Register("System.Blockchain.GetTransactionHeight", Blockchain_GetTransactionHeight, 0_01000000, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetTransactionFromBlock = Register("System.Blockchain.GetTransactionFromBlock", Blockchain_GetTransactionFromBlock, 0_01000000, TriggerType.Application, CallFlags.None);
            public static readonly InteropDescriptor GetContract = Register("System.Blockchain.GetContract", Blockchain_GetContract, 0_01000000, TriggerType.Application, CallFlags.None);

            private static bool Blockchain_GetHeight(ApplicationEngine engine)
            {
                engine.CurrentContext.EvaluationStack.Push(engine.Snapshot.Height);
                return true;
            }

            private static bool Blockchain_GetBlock(ApplicationEngine engine)
            {
                UInt256 hash;
                if (engine.TryPop(out uint height))
                {
                    hash = Ledger.Blockchain.Singleton.GetBlockHash(height);
                }
                else if (engine.TryPop(out ReadOnlySpan<byte> data))
                {
                    if (data.Length != 32) return false;
                    hash = new UInt256(data);
                }
                else
                {
                    return false;
                }
                Block block = hash != null ? engine.Snapshot.GetBlock(hash) : null;
                if (block != null && !IsTraceableBlock(engine.Snapshot, block.Index)) block = null;
                engine.Push(block?.ToStackItem(engine.ReferenceCounter) ?? StackItem.Null);
                return true;
            }

            private static bool Blockchain_GetTransaction(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> hash)) return false;
                TransactionState state = engine.Snapshot.Transactions.TryGet(new UInt256(hash));
                if (state != null && !IsTraceableBlock(engine.Snapshot, state.BlockIndex)) state = null;
                engine.Push(state?.Transaction.ToStackItem(engine.ReferenceCounter) ?? StackItem.Null);
                return true;
            }

            private static bool Blockchain_GetTransactionHeight(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> hash)) return false;
                TransactionState state = engine.Snapshot.Transactions.TryGet(new UInt256(hash));
                if (state != null && !IsTraceableBlock(engine.Snapshot, state.BlockIndex)) state = null;
                engine.Push(state?.BlockIndex ?? BigInteger.MinusOne);
                return true;
            }

            private static bool Blockchain_GetTransactionFromBlock(ApplicationEngine engine)
            {
                UInt256 hash;
                if (engine.TryPop(out uint height))
                {
                    hash = Ledger.Blockchain.Singleton.GetBlockHash(height);
                }
                else if (engine.TryPop(out ReadOnlySpan<byte> data))
                {
                    if (data.Length != 32) return false;
                    hash = new UInt256(data);
                }
                else
                {
                    return false;
                }
                TrimmedBlock block = hash != null ? engine.Snapshot.Blocks.TryGet(hash) : null;
                if (block != null && !IsTraceableBlock(engine.Snapshot, block.Index)) block = null;
                if (block is null)
                {
                    engine.Push(StackItem.Null);
                }
                else
                {
                    if (!engine.TryPop(out int index)) return false;
                    if (index < 0 || index >= block.Hashes.Length - 1) return false;
                    Transaction tx = engine.Snapshot.GetTransaction(block.Hashes[index + 1]);
                    engine.Push(tx?.ToStackItem(engine.ReferenceCounter) ?? StackItem.Null);
                }
                return true;
            }

            private static bool Blockchain_GetContract(ApplicationEngine engine)
            {
                UInt160 hash = new UInt160(engine.CurrentContext.EvaluationStack.Pop().GetSpan());
                ContractState contract = engine.Snapshot.Contracts.TryGet(hash);
                if (contract == null)
                    engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
                else
                    engine.CurrentContext.EvaluationStack.Push(contract.ToStackItem(engine.ReferenceCounter));
                return true;
            }

            private static bool IsTraceableBlock(StoreView snapshot, uint index)
            {
                if (index > snapshot.Height) return false;
                return index + MaxTraceableBlocks > snapshot.Height;
            }
        }
    }
}
