using AntShares.Core;
using System;
using System.Numerics;

namespace AntShares.VM
{
    internal class InterfaceEngine : ApiService
    {
        public static readonly InterfaceEngine Default = new InterfaceEngine();

        public override bool Invoke(string method, ScriptEngine engine)
        {
            switch (method)
            {
                case "AntShares.Blockchain.GetHeight":
                    return Blockchain_GetHeight(engine);
                case "AntShares.Blockchain.GetHeader":
                    return Blockchain_GetHeader(engine);
                case "AntShares.Blockchain.GetBlock":
                    return Blockchain_GetBlock(engine);
                case "AntShares.Blockchain.GetTransaction":
                    return Blockchain_GetTransaction(engine);
                case "AntShares.Header.GetHash":
                    return Header_GetHash(engine);
                case "AntShares.Header.GetVersion":
                    return Header_GetVersion(engine);
                case "AntShares.Header.GetPrevHash":
                    return Header_GetPrevHash(engine);
                case "AntShares.Header.GetMerkleRoot":
                    return Header_GetMerkleRoot(engine);
                case "AntShares.Header.GetTimestamp":
                    return Header_GetTimestamp(engine);
                case "AntShares.Header.GetNonce":
                    return Header_GetNonce(engine);
                case "AntShares.Header.GetNextMiner":
                    return Header_GetNextMiner(engine);
                case "AntShares.Block.GetTransactionCount":
                    return Block_GetTransactionCount(engine);
                case "AntShares.Block.GetTransactions":
                    return Block_GetTransactions(engine);
                case "AntShares.Block.GetTransaction":
                    return Block_GetTransaction(engine);
                case "AntShares.Transaction.GetHash":
                    return Transaction_GetHash(engine);
                case "AntShares.Transaction.GetType":
                    return Transaction_GetType(engine);
                case "AntShares.Asset.GetAssetType":
                    return Asset_GetAssetType(engine);
                case "AntShares.Asset.GetAmount":
                    return Asset_GetAmount(engine);
                case "AntShares.Asset.GetIssuer":
                    return Asset_GetIssuer(engine);
                case "AntShares.Asset.GetAdmin":
                    return Asset_GetAdmin(engine);
                case "AntShares.Enrollment.GetPublicKey":
                    return Enrollment_GetPublicKey(engine);
                case "AntShares.Transaction.GetAttributes":
                    return Transaction_GetAttributes(engine);
                case "AntShares.Transaction.GetInputs":
                    return Transaction_GetInputs(engine);
                case "AntShares.Transaction.GetOutputs":
                    return Transaction_GetOutputs(engine);
                case "AntShares.Transaction.GetReferences":
                    return Transaction_GetReferences(engine);
                case "AntShares.Attribute.GetUsage":
                    return Attribute_GetUsage(engine);
                case "AntShares.Attribute.GetData":
                    return Attribute_GetData(engine);
                case "AntShares.Input.GetHash":
                    return Input_GetHash(engine);
                case "AntShares.Input.GetIndex":
                    return Input_GetIndex(engine);
                case "AntShares.Output.GetAssetId":
                    return Output_GetAssetId(engine);
                case "AntShares.Output.GetValue":
                    return Output_GetValue(engine);
                case "AntShares.Output.GetScriptHash":
                    return Output_GetScriptHash(engine);
                default:
                    return base.Invoke(method, engine);
            }
        }

        private bool Blockchain_GetHeight(ScriptEngine engine)
        {
            if (Blockchain.Default == null)
                engine.EvaluationStack.Push(0);
            else
                engine.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        private bool Blockchain_GetHeader(ScriptEngine engine)
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

        private bool Blockchain_GetBlock(ScriptEngine engine)
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

        private bool Blockchain_GetTransaction(ScriptEngine engine)
        {
            byte[] hash = (byte[])engine.EvaluationStack.Pop();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.EvaluationStack.Push(new StackItem(tx));
            return true;
        }

        private bool Header_GetHash(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Hash.ToArray());
            return true;
        }

        private bool Header_GetVersion(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Version);
            return true;
        }

        private bool Header_GetPrevHash(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.PrevBlock.ToArray());
            return true;
        }

        private bool Header_GetMerkleRoot(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.MerkleRoot.ToArray());
            return true;
        }

        private bool Header_GetTimestamp(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Timestamp);
            return true;
        }

        private bool Header_GetNonce(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.ConsensusData);
            return true;
        }

        private bool Header_GetNextMiner(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.NextMiner.ToArray());
            return true;
        }

        private bool Block_GetTransactionCount(ScriptEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private bool Block_GetTransactions(ScriptEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            for (int i = block.Transactions.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(block.Transactions[i]));
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private bool Block_GetTransaction(ScriptEngine engine)
        {
            int index = (int)(BigInteger)engine.EvaluationStack.Pop();
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            if (index < 0 || index >= block.Transactions.Length) return false;
            Transaction tx = block.Transactions[index];
            engine.EvaluationStack.Push(new StackItem(tx));
            return true;
        }

        private bool Transaction_GetHash(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Hash.ToArray());
            return true;
        }

        private bool Transaction_GetType(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push((int)tx.Type);
            return true;
        }

        private bool Asset_GetAssetType(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push((int)asset.AssetType);
            return true;
        }

        private bool Asset_GetAmount(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Amount.GetData());
            return true;
        }

        private bool Asset_GetIssuer(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Issuer.EncodePoint(true));
            return true;
        }

        private bool Asset_GetAdmin(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Admin.ToArray());
            return true;
        }

        private bool Enrollment_GetPublicKey(ScriptEngine engine)
        {
            EnrollmentTransaction tx = engine.EvaluationStack.Pop().GetInterface<EnrollmentTransaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.PublicKey.EncodePoint(true));
            return true;
        }

        private bool Transaction_GetAttributes(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Attributes.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Attributes[i]));
            engine.EvaluationStack.Push(tx.Attributes.Length);
            return true;
        }

        private bool Transaction_GetInputs(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Inputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Inputs[i]));
            engine.EvaluationStack.Push(tx.Inputs.Length);
            return true;
        }

        private bool Transaction_GetOutputs(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Outputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Outputs[i]));
            engine.EvaluationStack.Push(tx.Outputs.Length);
            return true;
        }

        private bool Transaction_GetReferences(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Inputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.References[tx.Inputs[i]]));
            engine.EvaluationStack.Push(tx.Inputs.Length);
            return true;
        }

        private bool Attribute_GetUsage(ScriptEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push((int)attr.Usage);
            return true;
        }

        private bool Attribute_GetData(ScriptEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push(attr.Data);
            return true;
        }

        private bool Input_GetHash(ScriptEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        private bool Input_GetIndex(ScriptEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }

        private bool Output_GetAssetId(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.AssetId.ToArray());
            return true;
        }

        private bool Output_GetValue(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.Value.GetData());
            return true;
        }

        private bool Output_GetScriptHash(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }
    }
}
