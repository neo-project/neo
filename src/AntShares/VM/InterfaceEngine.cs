using AntShares.Core;
using System;
using System.Numerics;

namespace AntShares.VM
{
    internal class InterfaceEngine : ApiService
    {
        public static readonly InterfaceEngine Default = new InterfaceEngine();

        public InterfaceEngine()
        {
            Register("AntShares.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("AntShares.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("AntShares.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("AntShares.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("AntShares.Header.GetHash", Header_GetHash);
            Register("AntShares.Header.GetVersion", Header_GetVersion);
            Register("AntShares.Header.GetPrevHash", Header_GetPrevHash);
            Register("AntShares.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("AntShares.Header.GetTimestamp", Header_GetTimestamp);
            Register("AntShares.Header.GetNonce", Header_GetNonce);
            Register("AntShares.Header.GetNextMiner", Header_GetNextMiner);
            Register("AntShares.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("AntShares.Block.GetTransactions", Block_GetTransactions);
            Register("AntShares.Block.GetTransaction", Block_GetTransaction);
            Register("AntShares.Transaction.GetHash", Transaction_GetHash);
            Register("AntShares.Transaction.GetType", Transaction_GetType);
            Register("AntShares.Asset.GetAssetType", Asset_GetAssetType);
            Register("AntShares.Asset.GetAmount", Asset_GetAmount);
            Register("AntShares.Asset.GetIssuer", Asset_GetIssuer);
            Register("AntShares.Asset.GetAdmin", Asset_GetAdmin);
            Register("AntShares.Enrollment.GetPublicKey", Enrollment_GetPublicKey);
            Register("AntShares.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("AntShares.Transaction.GetInputs", Transaction_GetInputs);
            Register("AntShares.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("AntShares.Transaction.GetReferences", Transaction_GetReferences);
            Register("AntShares.Attribute.GetUsage", Attribute_GetUsage);
            Register("AntShares.Attribute.GetData", Attribute_GetData);
            Register("AntShares.Input.GetHash", Input_GetHash);
            Register("AntShares.Input.GetIndex", Input_GetIndex);
            Register("AntShares.Output.GetAssetId", Output_GetAssetId);
            Register("AntShares.Output.GetValue", Output_GetValue);
            Register("AntShares.Output.GetScriptHash", Output_GetScriptHash);
        }

        private static bool Blockchain_GetHeight(ScriptEngine engine)
        {
            if (Blockchain.Default == null)
                engine.EvaluationStack.Push(0);
            else
                engine.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        private static bool Blockchain_GetHeader(ScriptEngine engine)
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

        private static bool Blockchain_GetBlock(ScriptEngine engine)
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

        private static bool Blockchain_GetTransaction(ScriptEngine engine)
        {
            byte[] hash = (byte[])engine.EvaluationStack.Pop();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.EvaluationStack.Push(new StackItem(tx));
            return true;
        }

        private static bool Header_GetHash(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Hash.ToArray());
            return true;
        }

        private static bool Header_GetVersion(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Version);
            return true;
        }

        private static bool Header_GetPrevHash(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.PrevBlock.ToArray());
            return true;
        }

        private static bool Header_GetMerkleRoot(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.MerkleRoot.ToArray());
            return true;
        }

        private static bool Header_GetTimestamp(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Timestamp);
            return true;
        }

        private static bool Header_GetNonce(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.ConsensusData);
            return true;
        }

        private static bool Header_GetNextMiner(ScriptEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.NextMiner.ToArray());
            return true;
        }

        private static bool Block_GetTransactionCount(ScriptEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private static bool Block_GetTransactions(ScriptEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            for (int i = block.Transactions.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(block.Transactions[i]));
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private static bool Block_GetTransaction(ScriptEngine engine)
        {
            int index = (int)(BigInteger)engine.EvaluationStack.Pop();
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            if (index < 0 || index >= block.Transactions.Length) return false;
            Transaction tx = block.Transactions[index];
            engine.EvaluationStack.Push(new StackItem(tx));
            return true;
        }

        private static bool Transaction_GetHash(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Hash.ToArray());
            return true;
        }

        private static bool Transaction_GetType(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push((int)tx.Type);
            return true;
        }

        private static bool Asset_GetAssetType(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push((int)asset.AssetType);
            return true;
        }

        private static bool Asset_GetAmount(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Amount.GetData());
            return true;
        }

        private static bool Asset_GetIssuer(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Issuer.EncodePoint(true));
            return true;
        }

        private static bool Asset_GetAdmin(ScriptEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Admin.ToArray());
            return true;
        }

        private static bool Enrollment_GetPublicKey(ScriptEngine engine)
        {
            EnrollmentTransaction tx = engine.EvaluationStack.Pop().GetInterface<EnrollmentTransaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.PublicKey.EncodePoint(true));
            return true;
        }

        private static bool Transaction_GetAttributes(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Attributes.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Attributes[i]));
            engine.EvaluationStack.Push(tx.Attributes.Length);
            return true;
        }

        private static bool Transaction_GetInputs(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Inputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Inputs[i]));
            engine.EvaluationStack.Push(tx.Inputs.Length);
            return true;
        }

        private static bool Transaction_GetOutputs(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Outputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.Outputs[i]));
            engine.EvaluationStack.Push(tx.Outputs.Length);
            return true;
        }

        private static bool Transaction_GetReferences(ScriptEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            for (int i = tx.Inputs.Length - 1; i >= 0; i--)
                engine.EvaluationStack.Push(new StackItem(tx.References[tx.Inputs[i]]));
            engine.EvaluationStack.Push(tx.Inputs.Length);
            return true;
        }

        private static bool Attribute_GetUsage(ScriptEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push((int)attr.Usage);
            return true;
        }

        private static bool Attribute_GetData(ScriptEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push(attr.Data);
            return true;
        }

        private static bool Input_GetHash(ScriptEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        private static bool Input_GetIndex(ScriptEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }

        private static bool Output_GetAssetId(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.AssetId.ToArray());
            return true;
        }

        private static bool Output_GetValue(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.Value.GetData());
            return true;
        }

        private static bool Output_GetScriptHash(ScriptEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }
    }
}
