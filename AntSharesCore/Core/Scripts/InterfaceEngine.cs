using AntShares.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AntShares.Core.Scripts
{
    internal class InterfaceEngine : IApiService
    {
        public static readonly InterfaceEngine Default = new InterfaceEngine();

        public bool Invoke(string method, ScriptEngine engine)
        {
            switch (method)
            {
                case "System.now":
                    return SystemNow(engine);
                case "System.currentTx":
                    return SystemCurrentTx(engine);
                case "System.currentScriptHash":
                    return SystemCurrentScriptHash(engine);
                case "Chain.height":
                    return ChainHeight(engine);
                case "Chain.getHeader":
                    return ChainGetHeader(engine);
                case "Chain.getBlock":
                    return ChainGetBlock(engine);
                case "Chain.getTx":
                    return ChainGetTx(engine);
                case "Header.hash":
                    return HeaderHash(engine);
                case "Header.version":
                    return HeaderVersion(engine);
                case "Header.prevHash":
                    return HeaderPrevHash(engine);
                case "Header.merkleRoot":
                    return HeaderMerkleRoot(engine);
                case "Header.timestamp":
                    return HeaderTimestamp(engine);
                case "Header.nonce":
                    return HeaderNonce(engine);
                case "Header.nextMiner":
                    return HeaderNextMiner(engine);
                case "Block.txCount":
                    return BlockTxCount(engine);
                case "Block.tx":
                    return BlockTx(engine);
                case "Block.getTx":
                    return BlockGetTx(engine);
                case "TX.hash":
                    return TxHash(engine);
                case "TX.type":
                    return TxType(engine);
                case "Asset.type":
                    return AssetType(engine);
                case "Asset.amount":
                    return AssetAmount(engine);
                case "Asset.issuer":
                    return AssetIssuer(engine);
                case "Asset.admin":
                    return AssetAdmin(engine);
                case "Enroll.pubkey":
                    return EnrollPubkey(engine);
                case "Vote.enrollments":
                    return VoteEnrollments(engine);
                case "TX.attributes":
                    return TxAttributes(engine);
                case "TX.inputs":
                    return TxInputs(engine);
                case "TX.outputs":
                    return TxOutputs(engine);
                case "Attribute.usage":
                    return AttrUsage(engine);
                case "Attribute.data":
                    return AttrData(engine);
                case "Input.hash":
                    return TxInHash(engine);
                case "Input.index":
                    return TxInIndex(engine);
                case "Output.asset":
                    return TxOutAsset(engine);
                case "Output.value":
                    return TxOutValue(engine);
                case "Output.scriptHash":
                    return TxOutScriptHash(engine);
                default:
                    return false;
            }
        }

        private bool SystemNow(ScriptEngine engine)
        {
            engine.Stack.Push(DateTime.Now.ToTimestamp());
            return true;
        }

        private bool SystemCurrentTx(ScriptEngine engine)
        {
            engine.Stack.Push(new StackItem(engine.Signable as Transaction));
            return true;
        }

        private bool SystemCurrentScriptHash(ScriptEngine engine)
        {
            engine.Stack.Push(new StackItem(engine.ExecutingScript.ToScriptHash()));
            return true;
        }

        private bool ChainHeight(ScriptEngine engine)
        {
            if (Blockchain.Default == null)
                engine.Stack.Push(0);
            else
                engine.Stack.Push(Blockchain.Default.Height);
            return true;
        }

        private bool ChainGetHeader(ScriptEngine engine)
        {
            if (engine.Stack.Count < 1) return false;
            StackItem x = engine.Stack.Pop();
            byte[][] data = x.GetBytesArray();
            List<Block> r = new List<Block>();
            foreach (byte[] d in data)
            {
                switch (d.Length)
                {
                    case sizeof(uint):
                        uint height = BitConverter.ToUInt32(d, 0);
                        if (Blockchain.Default != null)
                            r.Add(Blockchain.Default.GetHeader(height));
                        else if (height == 0)
                            r.Add(Blockchain.GenesisBlock.Header);
                        else
                            r.Add(null);
                        break;
                    case 32:
                        UInt256 hash = new UInt256(d);
                        if (Blockchain.Default != null)
                            r.Add(Blockchain.Default.GetHeader(hash));
                        else if (hash == Blockchain.GenesisBlock.Hash)
                            r.Add(Blockchain.GenesisBlock.Header);
                        else
                            r.Add(null);
                        break;
                    default:
                        return false;
                }
            }
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r.ToArray()));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool ChainGetBlock(ScriptEngine engine)
        {
            if (engine.Stack.Count < 1) return false;
            StackItem x = engine.Stack.Pop();
            byte[][] data = x.GetBytesArray();
            List<Block> r = new List<Block>();
            foreach (byte[] d in data)
            {
                switch (d.Length)
                {
                    case sizeof(uint):
                        uint height = BitConverter.ToUInt32(d, 0);
                        if (Blockchain.Default != null)
                            r.Add(Blockchain.Default.GetBlock(height));
                        else if (height == 0)
                            r.Add(Blockchain.GenesisBlock);
                        else
                            r.Add(null);
                        break;
                    case 32:
                        UInt256 hash = new UInt256(d);
                        if (Blockchain.Default != null)
                            r.Add(Blockchain.Default.GetBlock(hash));
                        else if (hash == Blockchain.GenesisBlock.Hash)
                            r.Add(Blockchain.GenesisBlock);
                        else
                            r.Add(null);
                        break;
                    default:
                        return false;
                }
            }
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r.ToArray()));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool ChainGetTx(ScriptEngine engine)
        {
            if (engine.Stack.Count < 1) return false;
            StackItem x = engine.Stack.Pop();
            Transaction[] r = x.GetArray<UInt256>().Select(p => Blockchain.Default?.GetTransaction(p)).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderHash(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt256[] r = headers.Select(p => p.Hash).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderVersion(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            uint[] r = headers.Select(p => p.Version).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool HeaderPrevHash(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt256[] r = headers.Select(p => p.PrevBlock).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderMerkleRoot(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt256[] r = headers.Select(p => p.MerkleRoot).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderTimestamp(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            uint[] r = headers.Select(p => p.Timestamp).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool HeaderNonce(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            ulong[] r = headers.Select(p => p.Nonce).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool HeaderNextMiner(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt160[] r = headers.Select(p => p.NextMiner).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool BlockTxCount(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Block[] blocks = x.GetArray<Block>();
            if (blocks.Any(p => p == null || p.IsHeader)) return false;
            int[] r = blocks.Select(p => p.Transactions.Length).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool BlockTx(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            Block block = engine.AltStack.Peek().GetInterface<Block>();
            if (block == null || block.IsHeader) return false;
            engine.Stack.Push(new StackItem(block.Transactions));
            return true;
        }

        private bool BlockGetTx(ScriptEngine engine)
        {
            if (engine.Stack.Count < 1 || engine.AltStack.Count < 1) return false;
            StackItem block_item = engine.AltStack.Peek();
            Block[] blocks = block_item.GetArray<Block>();
            if (blocks.Any(p => p == null || p.IsHeader)) return false;
            StackItem index_item = engine.Stack.Pop();
            BigInteger[] indexes = index_item.GetIntArray();
            if (block_item.IsArray && index_item.IsArray && blocks.Length != indexes.Length)
                return false;
            if (blocks.Length == 1)
                blocks = Enumerable.Repeat(blocks[0], indexes.Length).ToArray();
            else if (indexes.Length == 1)
                indexes = Enumerable.Repeat(indexes[0], blocks.Length).ToArray();
            Transaction[] tx = blocks.Zip(indexes, (b, i) => i >= b.Transactions.Length ? null : b.Transactions[(int)i]).ToArray();
            if (block_item.IsArray || index_item.IsArray)
                engine.Stack.Push(new StackItem(tx));
            else
                engine.Stack.Push(new StackItem(tx[0]));
            return true;
        }

        private bool TxHash(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Transaction[] tx = x.GetArray<Transaction>();
            if (tx.Any(p => p == null)) return false;
            UInt256[] r = tx.Select(p => p.Hash).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool TxType(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            Transaction[] tx = x.GetArray<Transaction>();
            if (tx.Any(p => p == null)) return false;
            byte[][] r = tx.Select(p => new[] { (byte)p.Type }).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool AssetType(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            byte[][] r = tx.Select(p => new[] { (byte)p.AssetType }).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool AssetAmount(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            long[] r = tx.Select(p => p.Amount.GetData()).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool AssetIssuer(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            ECPoint[] r = tx.Select(p => p.Issuer).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool AssetAdmin(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            UInt160[] r = tx.Select(p => p.Admin).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool EnrollPubkey(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            EnrollmentTransaction[] tx = x.GetArray<EnrollmentTransaction>();
            if (tx.Any(p => p == null)) return false;
            ECPoint[] r = tx.Select(p => p.PublicKey).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool VoteEnrollments(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            VotingTransaction tx = engine.AltStack.Pop().GetInterface<VotingTransaction>();
            if (tx == null) return false;
            engine.Stack.Push(new StackItem(tx.Enrollments));
            return true;
        }

        private bool TxAttributes(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            Transaction tx = engine.AltStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.Stack.Push(new StackItem(tx.Attributes));
            return true;
        }

        private bool TxInputs(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            Transaction tx = engine.AltStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.Stack.Push(new StackItem(tx.Inputs));
            return true;
        }

        private bool TxOutputs(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            Transaction tx = engine.AltStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.Stack.Push(new StackItem(tx.Outputs));
            return true;
        }

        private bool AttrUsage(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionAttribute[] attr = x.GetArray<TransactionAttribute>();
            if (attr.Any(p => p == null)) return false;
            byte[][] r = attr.Select(p => new[] { (byte)p.Usage }).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool AttrData(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionAttribute[] attr = x.GetArray<TransactionAttribute>();
            if (attr.Any(p => p == null)) return false;
            byte[][] r = attr.Select(p => p.Data).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool TxInHash(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionInput[] inputs = x.GetArray<TransactionInput>();
            if (inputs.Any(p => p == null)) return false;
            UInt256[] r = inputs.Select(p => p.PrevHash).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool TxInIndex(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionInput[] inputs = x.GetArray<TransactionInput>();
            if (inputs.Any(p => p == null)) return false;
            uint[] r = inputs.Select(p => (uint)p.PrevIndex).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool TxOutAsset(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionOutput[] outputs = x.GetArray<TransactionOutput>();
            if (outputs.Any(p => p == null)) return false;
            UInt256[] r = outputs.Select(p => p.AssetId).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool TxOutValue(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionOutput[] outputs = x.GetArray<TransactionOutput>();
            if (outputs.Any(p => p == null)) return false;
            long[] r = outputs.Select(p => p.Value.GetData()).ToArray();
            if (x.IsArray)
                engine.Stack.Push(r);
            else
                engine.Stack.Push(r[0]);
            return true;
        }

        private bool TxOutScriptHash(ScriptEngine engine)
        {
            if (engine.AltStack.Count < 1) return false;
            StackItem x = engine.AltStack.Peek();
            TransactionOutput[] outputs = x.GetArray<TransactionOutput>();
            if (outputs.Any(p => p == null)) return false;
            UInt160[] r = outputs.Select(p => p.ScriptHash).ToArray();
            if (x.IsArray)
                engine.Stack.Push(new StackItem(r));
            else
                engine.Stack.Push(new StackItem(r[0]));
            return true;
        }
    }
}
