using AntShares.Core;
using System;
using System.Numerics;

namespace AntShares.VM
{
    internal class InterfaceEngine : IApiService
    {
        public static readonly InterfaceEngine Default = new InterfaceEngine();

        public bool Invoke(string method, ScriptEngine engine)
        {
            switch (method)
            {
                //case "System.now":
                //    return SystemNow(engine);
                case "System.currentTx":
                    return SystemCurrentTx(engine);
                case "System.currentScriptHash":
                    return SystemCurrentScriptHash(engine);
                case "AntShares.Chain.height":
                    return ChainHeight(engine);
                case "AntShares.Chain.getHeader":
                    return ChainGetHeader(engine);
                case "AntShares.Chain.getBlock":
                    return ChainGetBlock(engine);
                case "AntShares.Chain.getTx":
                    return ChainGetTx(engine);
                case "AntShares.Header.hash":
                    return HeaderHash(engine);
                case "AntShares.Header.version":
                    return HeaderVersion(engine);
                case "AntShares.Header.prevHash":
                    return HeaderPrevHash(engine);
                case "AntShares.Header.merkleRoot":
                    return HeaderMerkleRoot(engine);
                case "AntShares.Header.timestamp":
                    return HeaderTimestamp(engine);
                case "AntShares.Header.nonce":
                    return HeaderNonce(engine);
                case "AntShares.Header.nextMiner":
                    return HeaderNextMiner(engine);
                case "AntShares.Block.txCount":
                    return BlockTxCount(engine);
                case "AntShares.Block.tx":
                    return BlockTx(engine);
                case "AntShares.Block.getTx":
                    return BlockGetTx(engine);
                case "AntShares.TX.hash":
                    return TxHash(engine);
                case "AntShares.TX.type":
                    return TxType(engine);
                case "AntShares.Asset.type":
                    return AssetType(engine);
                case "AntShares.Asset.amount":
                    return AssetAmount(engine);
                case "AntShares.Asset.issuer":
                    return AssetIssuer(engine);
                case "AntShares.Asset.admin":
                    return AssetAdmin(engine);
                case "AntShares.Enroll.pubkey":
                    return EnrollPubkey(engine);
                case "AntShares.TX.attributes":
                    return TxAttributes(engine);
                case "AntShares.TX.inputs":
                    return TxInputs(engine);
                case "AntShares.TX.outputs":
                    return TxOutputs(engine);
                case "AntShares.Attribute.usage":
                    return AttrUsage(engine);
                case "AntShares.Attribute.data":
                    return AttrData(engine);
                case "AntShares.Input.hash":
                    return TxInHash(engine);
                case "AntShares.Input.index":
                    return TxInIndex(engine);
                case "AntShares.Output.asset":
                    return TxOutAsset(engine);
                case "AntShares.Output.value":
                    return TxOutValue(engine);
                case "AntShares.Output.scriptHash":
                    return TxOutScriptHash(engine);
                default:
                    return false;
            }
        }

        private bool SystemNow(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(DateTime.Now.ToTimestamp());
            return true;
        }

        private bool SystemCurrentTx(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(new StackItem(engine.Signable as Transaction));
            return true;
        }

        private bool SystemCurrentScriptHash(ScriptEngine engine)
        {
            engine.EvaluationStack.Push(new StackItem(engine.ExecutingScript.ToScriptHash().ToArray()));
            return true;
        }

        private bool ChainHeight(ScriptEngine engine)
        {
            if (Blockchain.Default == null)
                engine.EvaluationStack.Push(0);
            else
                engine.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        private bool ChainGetHeader(ScriptEngine engine)
        {
            byte[] data = (byte[])engine.EvaluationStack.Pop();
            Header header;
            switch (data.Length)
            {
                case sizeof(uint):
                    uint height = BitConverter.ToUInt32(data, 0);
                    if (Blockchain.Default != null)
                        header = Blockchain.Default.GetHeader(height);
                    else if (height == 0)
                        header = Blockchain.GenesisBlock.Header;
                    else
                        header = null;
                    break;
                case 32:
                    UInt256 hash = new UInt256(data);
                    if (Blockchain.Default != null)
                        header = Blockchain.Default.GetHeader(hash);
                    else if (hash == Blockchain.GenesisBlock.Hash)
                        header = Blockchain.GenesisBlock.Header;
                    else
                        header = null;
                    break;
                default:
                    return false;
            }
            engine.EvaluationStack.Push(new StackItem(header));
            return true;
        }

        private bool ChainGetBlock(ScriptEngine engine)
        {
            byte[] data = (byte[])engine.EvaluationStack.Pop();
            Block block;
            switch (data.Length)
            {
                case sizeof(uint):
                    uint height = BitConverter.ToUInt32(data, 0);
                    if (Blockchain.Default != null)
                        block = Blockchain.Default.GetBlock(height);
                    else if (height == 0)
                        block = Blockchain.GenesisBlock;
                    else
                        block = null;
                    break;
                case 32:
                    UInt256 hash = new UInt256(data);
                    if (Blockchain.Default != null)
                        block = Blockchain.Default.GetBlock(hash);
                    else if (hash == Blockchain.GenesisBlock.Hash)
                        block = Blockchain.GenesisBlock;
                    else
                        block = null;
                    break;
                default:
                    return false;
            }
            engine.EvaluationStack.Push(new StackItem(block));
            return true;
        }

        private bool ChainGetTx(ScriptEngine engine)
        {
            byte[] hash = (byte[])engine.EvaluationStack.Pop();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.EvaluationStack.Push(new StackItem(tx));
            return true;
        }

        private bool HeaderHash(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Hash.ToArray());
            return true;
        }

        private bool HeaderVersion(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Version);
            return true;
        }

        private bool HeaderPrevHash(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.PrevBlock.ToArray());
            return true;
        }

        private bool HeaderMerkleRoot(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.MerkleRoot.ToArray());
            return true;
        }

        private bool HeaderTimestamp(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Timestamp);
            return true;
        }

        private bool HeaderNonce(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.ConsensusData);
            return true;
        }

        private bool HeaderNextMiner(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.NextMiner.ToArray());
            return true;
        }

        private bool BlockTxCount(ScriptEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private bool BlockTx(ScriptEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            for (int i = block.Transactions.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(block.Transactions[i]));
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private bool BlockGetTx(ScriptEngine engine)
        {
            int index = (int)(BigInteger)engine.EvaluationStack.Pop();
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            if (index < 0 || index >= block.Transactions.Length) return false;
            Transaction tx = block.Transactions[index];
            engine.EvaluationStack.Push(new StackItem(tx));
            return true;
        }

        private bool TxHash(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Hash.ToArray());
            return true;
        }

        private bool TxType(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push((int)tx.Type);
            return true;
        }

        private bool AssetType(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push((int)asset.AssetType);
            return true;
        }

        private bool AssetAmount(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Amount.GetData());
            return true;
        }

        private bool AssetIssuer(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Issuer.EncodePoint(true));
            return true;
        }

        private bool AssetAdmin(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Admin.ToArray());
            return true;
        }

        private bool EnrollPubkey(ScriptEngine engine)
        {
            EnrollmentTransaction tx = engine.EvaluationStack.Pop().GetInterface<EnrollmentTransaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.PublicKey.EncodePoint(true));
            return true;
        }

        private bool TxAttributes(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Attributes.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Attributes[i]));
            engine.EvaluationStack.Push(tx.Attributes.Length);
            return true;
        }

        private bool TxInputs(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Inputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Inputs[i]));
            engine.EvaluationStack.Push(tx.Inputs.Length);
            return true;
        }

        private bool TxOutputs(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Outputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Outputs[i]));
            engine.EvaluationStack.Push(tx.Outputs.Length);
            return true;
        }

        private bool AttrUsage(ScriptEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push((int)attr.Usage);
            return true;
        }

        private bool AttrData(ScriptEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push(attr.Data);
            return true;
        }

        private bool TxInHash(ScriptEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        private bool TxInIndex(ScriptEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }

        private bool TxOutAsset(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.AssetId.ToArray());
            return true;
        }

        private bool TxOutValue(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.Value.GetData());
            return true;
        }

        private bool TxOutScriptHash(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }
    }
}
