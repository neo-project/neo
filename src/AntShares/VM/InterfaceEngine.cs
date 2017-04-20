using AntShares.Core;
using System.Linq;

namespace AntShares.VM
{
    internal class InterfaceEngine : InteropService
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
            Register("AntShares.Header.GetConsensusData", Header_GetConsensusData);
            Register("AntShares.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("AntShares.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("AntShares.Block.GetTransactions", Block_GetTransactions);
            Register("AntShares.Block.GetTransaction", Block_GetTransaction);
            Register("AntShares.Transaction.GetHash", Transaction_GetHash);
            Register("AntShares.Transaction.GetType", Transaction_GetType);
            Register("AntShares.Asset.GetAssetType", Asset_GetAssetType);
            Register("AntShares.Asset.GetAmount", Asset_GetAmount);
            Register("AntShares.Asset.GetOwner", Asset_GetOwner);
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

        private static bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            if (Blockchain.Default == null)
                engine.EvaluationStack.Push(0);
            else
                engine.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        private static bool Blockchain_GetHeader(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            Header header;
            switch (data.Length)
            {
                case sizeof(uint):
                    uint height = data.ToUInt32(0);
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
            engine.EvaluationStack.Push(StackItem.FromInterface(header));
            return true;
        }

        private static bool Blockchain_GetBlock(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            Block block;
            switch (data.Length)
            {
                case sizeof(uint):
                    uint height = data.ToUInt32(0);
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
            engine.EvaluationStack.Push(StackItem.FromInterface(block));
            return true;
        }

        private static bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            byte[] hash = engine.EvaluationStack.Pop().GetByteArray();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        private static bool Header_GetHash(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Hash.ToArray());
            return true;
        }

        private static bool Header_GetVersion(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Version);
            return true;
        }

        private static bool Header_GetPrevHash(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.PrevHash.ToArray());
            return true;
        }

        private static bool Header_GetMerkleRoot(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.MerkleRoot.ToArray());
            return true;
        }

        private static bool Header_GetTimestamp(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Timestamp);
            return true;
        }

        private static bool Header_GetConsensusData(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.ConsensusData);
            return true;
        }

        private static bool Header_GetNextConsensus(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.NextConsensus.ToArray());
            return true;
        }

        private static bool Block_GetTransactionCount(ExecutionEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        private static bool Block_GetTransactions(ExecutionEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        private static bool Block_GetTransaction(ExecutionEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            int index = (int)engine.EvaluationStack.Pop().GetBigInteger();
            if (block == null) return false;
            if (index < 0 || index >= block.Transactions.Length) return false;
            Transaction tx = block.Transactions[index];
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        private static bool Transaction_GetHash(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Hash.ToArray());
            return true;
        }

        private static bool Transaction_GetType(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push((int)tx.Type);
            return true;
        }

        private static bool Asset_GetAssetType(ExecutionEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push((int)asset.AssetType);
            return true;
        }

        private static bool Asset_GetAmount(ExecutionEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Amount.GetData());
            return true;
        }

        private static bool Asset_GetOwner(ExecutionEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Owner.EncodePoint(true));
            return true;
        }

        private static bool Asset_GetAdmin(ExecutionEngine engine)
        {
            RegisterTransaction asset = engine.EvaluationStack.Pop().GetInterface<RegisterTransaction>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Admin.ToArray());
            return true;
        }

        private static bool Enrollment_GetPublicKey(ExecutionEngine engine)
        {
            EnrollmentTransaction tx = engine.EvaluationStack.Pop().GetInterface<EnrollmentTransaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.PublicKey.EncodePoint(true));
            return true;
        }

        private static bool Transaction_GetAttributes(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Attributes.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        private static bool Transaction_GetInputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        private static bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        private static bool Transaction_GetReferences(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(tx.References[p])).ToArray());
            return true;
        }

        private static bool Attribute_GetUsage(ExecutionEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push((int)attr.Usage);
            return true;
        }

        private static bool Attribute_GetData(ExecutionEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push(attr.Data);
            return true;
        }

        private static bool Input_GetHash(ExecutionEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        private static bool Input_GetIndex(ExecutionEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }

        private static bool Output_GetAssetId(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.AssetId.ToArray());
            return true;
        }

        private static bool Output_GetValue(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.Value.GetData());
            return true;
        }

        private static bool Output_GetScriptHash(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }
    }
}
