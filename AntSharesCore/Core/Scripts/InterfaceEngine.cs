using AntShares.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AntShares.Core.Scripts
{
    internal class InterfaceEngine
    {
        private readonly Stack<StackItem> stack;
        private readonly Stack<StackItem> altStack;
        private readonly ISignable signable;

        public InterfaceEngine(Stack<StackItem> stack, Stack<StackItem> altStack, ISignable signable)
        {
            this.stack = stack;
            this.altStack = altStack;
            this.signable = signable;
        }

        public bool ExecuteOp(InterfaceOp code)
        {
            switch (code)
            {
                case InterfaceOp.SYSTEM_NOW:
                    return SystemNow();
                case InterfaceOp.SYSTEM_CURRENTTX:
                    return SystemCurrentTx();
                case InterfaceOp.CHAIN_HEIGHT:
                    return ChainHeight();
                case InterfaceOp.CHAIN_GETHEADER:
                    return ChainGetHeader();
                case InterfaceOp.CHAIN_GETBLOCK:
                    return ChainGetBlock();
                case InterfaceOp.CHAIN_GETTX:
                    return ChainGetTx();
                case InterfaceOp.HEADER_HASH:
                    return HeaderHash();
                case InterfaceOp.HEADER_VERSION:
                    return HeaderVersion();
                case InterfaceOp.HEADER_PREVHASH:
                    return HeaderPrevHash();
                case InterfaceOp.HEADER_MERKLEROOT:
                    return HeaderMerkleRoot();
                case InterfaceOp.HEADER_TIMESTAMP:
                    return HeaderTimestamp();
                case InterfaceOp.HEADER_NONCE:
                    return HeaderNonce();
                case InterfaceOp.HEADER_NEXTMINER:
                    return HeaderNextMiner();
                case InterfaceOp.BLOCK_TXCOUNT:
                    return BlockTxCount();
                case InterfaceOp.BLOCK_TX:
                    return BlockTx();
                case InterfaceOp.BLOCK_GETTX:
                    return BlockGetTx();
                case InterfaceOp.TX_HASH:
                    return TxHash();
                case InterfaceOp.TX_TYPE:
                    return TxType();
                case InterfaceOp.ASSET_TYPE:
                    return AssetType();
                case InterfaceOp.ASSET_AMOUNT:
                    return AssetAmount();
                case InterfaceOp.ASSET_ISSUER:
                    return AssetIssuer();
                case InterfaceOp.ASSET_ADMIN:
                    return AssetAdmin();
                case InterfaceOp.ENROLL_PUBKEY:
                    return EnrollPubkey();
                case InterfaceOp.VOTE_ENROLLMENTS:
                    return VoteEnrollments();
                case InterfaceOp.TX_ATTRIBUTES:
                    return TxAttributes();
                case InterfaceOp.TX_INPUTS:
                    return TxInputs();
                case InterfaceOp.TX_OUTPUTS:
                    return TxOutputs();
                case InterfaceOp.ATTR_USAGE:
                    return AttrUsage();
                case InterfaceOp.ATTR_DATA:
                    return AttrData();
                case InterfaceOp.TXIN_HASH:
                    return TxInHash();
                case InterfaceOp.TXIN_INDEX:
                    return TxInIndex();
                case InterfaceOp.TXOUT_ASSET:
                    return TxOutAsset();
                case InterfaceOp.TXOUT_VALUE:
                    return TxOutValue();
                case InterfaceOp.TXOUT_SCRIPTHASH:
                    return TxOutScriptHash();
                default:
                    return false;
            }
        }

        private bool SystemNow()
        {
            stack.Push(DateTime.Now.ToTimestamp());
            return true;
        }

        private bool SystemCurrentTx()
        {
            stack.Push(new StackItem(signable as Transaction));
            return true;
        }

        private bool ChainHeight()
        {
            if (Blockchain.Default == null)
                stack.Push(0);
            else
                stack.Push(Blockchain.Default.Height);
            return true;
        }

        private bool ChainGetHeader()
        {
            if (stack.Count < 1) return false;
            StackItem x = stack.Pop();
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
                stack.Push(new StackItem(r.ToArray()));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool ChainGetBlock()
        {
            if (stack.Count < 1) return false;
            StackItem x = stack.Pop();
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
                stack.Push(new StackItem(r.ToArray()));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool ChainGetTx()
        {
            if (stack.Count < 1) return false;
            StackItem x = stack.Pop();
            Transaction[] r = x.GetArray<UInt256>().Select(p => Blockchain.Default?.GetTransaction(p)).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderHash()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt256[] r = headers.Select(p => p.Hash).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderVersion()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            uint[] r = headers.Select(p => p.Version).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool HeaderPrevHash()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt256[] r = headers.Select(p => p.PrevBlock).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderMerkleRoot()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt256[] r = headers.Select(p => p.MerkleRoot).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool HeaderTimestamp()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            uint[] r = headers.Select(p => p.Timestamp).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool HeaderNonce()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            ulong[] r = headers.Select(p => p.Nonce).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool HeaderNextMiner()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] headers = x.GetArray<Block>();
            if (headers.Any(p => p == null)) return false;
            UInt160[] r = headers.Select(p => p.NextMiner).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool BlockTxCount()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Block[] blocks = x.GetArray<Block>();
            if (blocks.Any(p => p == null || p.IsHeader)) return false;
            int[] r = blocks.Select(p => p.Transactions.Length).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool BlockTx()
        {
            if (altStack.Count < 1) return false;
            Block block = altStack.Peek().GetInterface<Block>();
            if (block == null || block.IsHeader) return false;
            stack.Push(new StackItem(block.Transactions));
            return true;
        }

        private bool BlockGetTx()
        {
            if (stack.Count < 1 || altStack.Count < 1) return false;
            StackItem block_item = altStack.Peek();
            Block[] blocks = block_item.GetArray<Block>();
            if (blocks.Any(p => p == null || p.IsHeader)) return false;
            StackItem index_item = stack.Pop();
            BigInteger[] indexes = index_item.GetIntArray();
            if (block_item.IsArray && index_item.IsArray && blocks.Length != indexes.Length)
                return false;
            if (blocks.Length == 1)
                blocks = Enumerable.Repeat(blocks[0], indexes.Length).ToArray();
            else if (indexes.Length == 1)
                indexes = Enumerable.Repeat(indexes[0], blocks.Length).ToArray();
            Transaction[] tx = blocks.Zip(indexes, (b, i) => i >= b.Transactions.Length ? null : b.Transactions[(int)i]).ToArray();
            if (block_item.IsArray || index_item.IsArray)
                stack.Push(new StackItem(tx));
            else
                stack.Push(new StackItem(tx[0]));
            return true;
        }

        private bool TxHash()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Transaction[] tx = x.GetArray<Transaction>();
            if (tx.Any(p => p == null)) return false;
            UInt256[] r = tx.Select(p => p.Hash).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool TxType()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            Transaction[] tx = x.GetArray<Transaction>();
            if (tx.Any(p => p == null)) return false;
            byte[][] r = tx.Select(p => new[] { (byte)p.Type }).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool AssetType()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            byte[][] r = tx.Select(p => new[] { (byte)p.AssetType }).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool AssetAmount()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            long[] r = tx.Select(p => p.Amount.GetData()).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool AssetIssuer()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            ECPoint[] r = tx.Select(p => p.Issuer).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool AssetAdmin()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            RegisterTransaction[] tx = x.GetArray<RegisterTransaction>();
            if (tx.Any(p => p == null)) return false;
            UInt160[] r = tx.Select(p => p.Admin).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool EnrollPubkey()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            EnrollmentTransaction[] tx = x.GetArray<EnrollmentTransaction>();
            if (tx.Any(p => p == null)) return false;
            ECPoint[] r = tx.Select(p => p.PublicKey).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool VoteEnrollments()
        {
            if (altStack.Count < 1) return false;
            VotingTransaction tx = altStack.Pop().GetInterface<VotingTransaction>();
            if (tx == null) return false;
            stack.Push(new StackItem(tx.Enrollments));
            return true;
        }

        private bool TxAttributes()
        {
            if (altStack.Count < 1) return false;
            Transaction tx = altStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            stack.Push(new StackItem(tx.Attributes));
            return true;
        }

        private bool TxInputs()
        {
            if (altStack.Count < 1) return false;
            Transaction tx = altStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            stack.Push(new StackItem(tx.Inputs));
            return true;
        }

        private bool TxOutputs()
        {
            if (altStack.Count < 1) return false;
            Transaction tx = altStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            stack.Push(new StackItem(tx.Outputs));
            return true;
        }

        private bool AttrUsage()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionAttribute[] attr = x.GetArray<TransactionAttribute>();
            if (attr.Any(p => p == null)) return false;
            byte[][] r = attr.Select(p => new[] { (byte)p.Usage }).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool AttrData()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionAttribute[] attr = x.GetArray<TransactionAttribute>();
            if (attr.Any(p => p == null)) return false;
            byte[][] r = attr.Select(p => p.Data).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool TxInHash()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionInput[] inputs = x.GetArray<TransactionInput>();
            if (inputs.Any(p => p == null)) return false;
            UInt256[] r = inputs.Select(p => p.PrevHash).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool TxInIndex()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionInput[] inputs = x.GetArray<TransactionInput>();
            if (inputs.Any(p => p == null)) return false;
            uint[] r = inputs.Select(p => (uint)p.PrevIndex).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool TxOutAsset()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionOutput[] outputs = x.GetArray<TransactionOutput>();
            if (outputs.Any(p => p == null)) return false;
            UInt256[] r = outputs.Select(p => p.AssetId).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }

        private bool TxOutValue()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionOutput[] outputs = x.GetArray<TransactionOutput>();
            if (outputs.Any(p => p == null)) return false;
            long[] r = outputs.Select(p => p.Value.GetData()).ToArray();
            if (x.IsArray)
                stack.Push(r);
            else
                stack.Push(r[0]);
            return true;
        }

        private bool TxOutScriptHash()
        {
            if (altStack.Count < 1) return false;
            StackItem x = altStack.Peek();
            TransactionOutput[] outputs = x.GetArray<TransactionOutput>();
            if (outputs.Any(p => p == null)) return false;
            UInt160[] r = outputs.Select(p => p.ScriptHash).ToArray();
            if (x.IsArray)
                stack.Push(new StackItem(r));
            else
                stack.Push(new StackItem(r[0]));
            return true;
        }
    }
}
