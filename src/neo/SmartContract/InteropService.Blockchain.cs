using Neo.Ledger;
using Neo.Network.P2P.Payloads;
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
                ReadOnlySpan<byte> data = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                UInt256 hash;
                if (data.Length <= 5)
                    hash = Ledger.Blockchain.Singleton.GetBlockHash((uint)new BigInteger(data));
                else if (data.Length == 32)
                    hash = new UInt256(data);
                else
                    return false;

                Block block = hash != null ? engine.Snapshot.GetBlock(hash) : null;
                if (block == null)
                    engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
                else
                    engine.CurrentContext.EvaluationStack.Push(block.ToStackItem(engine.ReferenceCounter));
                return true;
            }

            private static bool Blockchain_GetTransaction(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> hash = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                Transaction tx = engine.Snapshot.GetTransaction(new UInt256(hash));
                if (tx == null)
                    engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
                else
                    engine.CurrentContext.EvaluationStack.Push(tx.ToStackItem(engine.ReferenceCounter));
                return true;
            }

            private static bool Blockchain_GetTransactionHeight(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> hash = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                var tx = engine.Snapshot.Transactions.TryGet(new UInt256(hash));
                engine.CurrentContext.EvaluationStack.Push(tx != null ? new BigInteger(tx.BlockIndex) : BigInteger.MinusOne);
                return true;
            }

            private static bool Blockchain_GetTransactionFromBlock(ApplicationEngine engine)
            {
                ReadOnlySpan<byte> data = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                UInt256 hash;
                if (data.Length <= 5)
                    hash = Ledger.Blockchain.Singleton.GetBlockHash((uint)new BigInteger(data));
                else if (data.Length == 32)
                    hash = new UInt256(data);
                else
                    return false;

                TrimmedBlock block = hash != null ? engine.Snapshot.Blocks.TryGet(hash) : null;
                if (block == null)
                {
                    engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
                }
                else
                {
                    int index = (int)engine.CurrentContext.EvaluationStack.Pop().GetBigInteger();
                    if (index < 0 || index >= block.Hashes.Length - 1) return false;

                    Transaction tx = engine.Snapshot.GetTransaction(block.Hashes[index + 1]);
                    if (tx == null)
                        engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
                    else
                        engine.CurrentContext.EvaluationStack.Push(tx.ToStackItem(engine.ReferenceCounter));
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
        }
    }
}
