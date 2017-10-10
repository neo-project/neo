using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.VM;
using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Neo.SmartContract
{
    public class StateReader : InteropService
    {
        public event EventHandler<NotifyEventArgs> Notify;
        public event EventHandler<LogEventArgs> Log;

        public static readonly StateReader Default = new StateReader();

        public StateReader()
        {
            Register("Neo.Runtime.GetTrigger", Runtime_GetTrigger);
            Register("Neo.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("Neo.Runtime.Notify", Runtime_Notify);
            Register("Neo.Runtime.Log", Runtime_Log);
            Register("Neo.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("Neo.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("Neo.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("Neo.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("Neo.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("Neo.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("Neo.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("Neo.Blockchain.GetContract", Blockchain_GetContract);
            Register("Neo.Header.GetHash", Header_GetHash);
            Register("Neo.Header.GetVersion", Header_GetVersion);
            Register("Neo.Header.GetPrevHash", Header_GetPrevHash);
            Register("Neo.Header.GetMerkleRoot", Header_GetMerkleRoot);
            Register("Neo.Header.GetTimestamp", Header_GetTimestamp);
            Register("Neo.Header.GetConsensusData", Header_GetConsensusData);
            Register("Neo.Header.GetNextConsensus", Header_GetNextConsensus);
            Register("Neo.Block.GetTransactionCount", Block_GetTransactionCount);
            Register("Neo.Block.GetTransactions", Block_GetTransactions);
            Register("Neo.Block.GetTransaction", Block_GetTransaction);
            Register("Neo.Transaction.GetHash", Transaction_GetHash);
            Register("Neo.Transaction.GetType", Transaction_GetType);
            Register("Neo.Transaction.GetAttributes", Transaction_GetAttributes);
            Register("Neo.Transaction.GetInputs", Transaction_GetInputs);
            Register("Neo.Transaction.GetOutputs", Transaction_GetOutputs);
            Register("Neo.Transaction.GetReferences", Transaction_GetReferences);
            Register("Neo.Attribute.GetUsage", Attribute_GetUsage);
            Register("Neo.Attribute.GetData", Attribute_GetData);
            Register("Neo.Input.GetHash", Input_GetHash);
            Register("Neo.Input.GetIndex", Input_GetIndex);
            Register("Neo.Output.GetAssetId", Output_GetAssetId);
            Register("Neo.Output.GetValue", Output_GetValue);
            Register("Neo.Output.GetScriptHash", Output_GetScriptHash);
            Register("Neo.Account.GetScriptHash", Account_GetScriptHash);
            Register("Neo.Account.GetVotes", Account_GetVotes);
            Register("Neo.Account.GetBalance", Account_GetBalance);
            Register("Neo.Asset.GetAssetId", Asset_GetAssetId);
            Register("Neo.Asset.GetAssetType", Asset_GetAssetType);
            Register("Neo.Asset.GetAmount", Asset_GetAmount);
            Register("Neo.Asset.GetAvailable", Asset_GetAvailable);
            Register("Neo.Asset.GetPrecision", Asset_GetPrecision);
            Register("Neo.Asset.GetOwner", Asset_GetOwner);
            Register("Neo.Asset.GetAdmin", Asset_GetAdmin);
            Register("Neo.Asset.GetIssuer", Asset_GetIssuer);
            Register("Neo.Contract.GetScript", Contract_GetScript);
            Register("Neo.Storage.GetContext", Storage_GetContext);
            Register("Neo.Storage.Get", Storage_Get);
            #region Old AntShares APIs
            Register("AntShares.Runtime.CheckWitness", Runtime_CheckWitness);
            Register("AntShares.Runtime.Notify", Runtime_Notify);
            Register("AntShares.Runtime.Log", Runtime_Log);
            Register("AntShares.Blockchain.GetHeight", Blockchain_GetHeight);
            Register("AntShares.Blockchain.GetHeader", Blockchain_GetHeader);
            Register("AntShares.Blockchain.GetBlock", Blockchain_GetBlock);
            Register("AntShares.Blockchain.GetTransaction", Blockchain_GetTransaction);
            Register("AntShares.Blockchain.GetAccount", Blockchain_GetAccount);
            Register("AntShares.Blockchain.GetValidators", Blockchain_GetValidators);
            Register("AntShares.Blockchain.GetAsset", Blockchain_GetAsset);
            Register("AntShares.Blockchain.GetContract", Blockchain_GetContract);
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
            Register("AntShares.Account.GetScriptHash", Account_GetScriptHash);
            Register("AntShares.Account.GetVotes", Account_GetVotes);
            Register("AntShares.Account.GetBalance", Account_GetBalance);
            Register("AntShares.Asset.GetAssetId", Asset_GetAssetId);
            Register("AntShares.Asset.GetAssetType", Asset_GetAssetType);
            Register("AntShares.Asset.GetAmount", Asset_GetAmount);
            Register("AntShares.Asset.GetAvailable", Asset_GetAvailable);
            Register("AntShares.Asset.GetPrecision", Asset_GetPrecision);
            Register("AntShares.Asset.GetOwner", Asset_GetOwner);
            Register("AntShares.Asset.GetAdmin", Asset_GetAdmin);
            Register("AntShares.Asset.GetIssuer", Asset_GetIssuer);
            Register("AntShares.Contract.GetScript", Contract_GetScript);
            Register("AntShares.Storage.GetContext", Storage_GetContext);
            Register("AntShares.Storage.Get", Storage_Get);
            #endregion
        }

        protected virtual bool Runtime_GetTrigger(ExecutionEngine engine)
        {
            ApplicationEngine app_engine = (ApplicationEngine)engine;
            engine.EvaluationStack.Push((int)app_engine.Trigger);
            return true;
        }

        protected bool CheckWitness(ExecutionEngine engine, UInt160 hash)
        {
            IVerifiable container = (IVerifiable)engine.ScriptContainer;
            UInt160[] _hashes_for_verifying = container.GetScriptHashesForVerifying();
            return _hashes_for_verifying.Contains(hash);
        }

        protected bool CheckWitness(ExecutionEngine engine, ECPoint pubkey)
        {
            return CheckWitness(engine, Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash());
        }

        protected virtual bool Runtime_CheckWitness(ExecutionEngine engine)
        {
            byte[] hashOrPubkey = engine.EvaluationStack.Pop().GetByteArray();
            bool result;
            if (hashOrPubkey.Length == 20)
                result = CheckWitness(engine, new UInt160(hashOrPubkey));
            else if (hashOrPubkey.Length == 33)
                result = CheckWitness(engine, ECPoint.DecodePoint(hashOrPubkey, ECCurve.Secp256r1));
            else
                return false;
            engine.EvaluationStack.Push(result);
            return true;
        }

        protected virtual bool Runtime_Notify(ExecutionEngine engine)
        {
            StackItem state = engine.EvaluationStack.Pop();
            Notify?.Invoke(this, new NotifyEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), state));
            return true;
        }

        protected virtual bool Runtime_Log(ExecutionEngine engine)
        {
            string message = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            Log?.Invoke(this, new LogEventArgs(engine.ScriptContainer, new UInt160(engine.CurrentContext.ScriptHash), message));
            return true;
        }

        protected virtual bool Blockchain_GetHeight(ExecutionEngine engine)
        {
            if (Blockchain.Default == null)
                engine.EvaluationStack.Push(0);
            else
                engine.EvaluationStack.Push(Blockchain.Default.Height);
            return true;
        }

        protected virtual bool Blockchain_GetHeader(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            Header header;
            if (data.Length <= 5)
            {
                uint height = (uint)new BigInteger(data);
                if (Blockchain.Default != null)
                    header = Blockchain.Default.GetHeader(height);
                else if (height == 0)
                    header = Blockchain.GenesisBlock.Header;
                else
                    header = null;
            }
            else if (data.Length == 32)
            {
                UInt256 hash = new UInt256(data);
                if (Blockchain.Default != null)
                    header = Blockchain.Default.GetHeader(hash);
                else if (hash == Blockchain.GenesisBlock.Hash)
                    header = Blockchain.GenesisBlock.Header;
                else
                    header = null;
            }
            else
            {
                return false;
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(header));
            return true;
        }

        protected virtual bool Blockchain_GetBlock(ExecutionEngine engine)
        {
            byte[] data = engine.EvaluationStack.Pop().GetByteArray();
            Block block;
            if (data.Length <= 5)
            {
                uint height = (uint)new BigInteger(data);
                if (Blockchain.Default != null)
                    block = Blockchain.Default.GetBlock(height);
                else if (height == 0)
                    block = Blockchain.GenesisBlock;
                else
                    block = null;
            }
            else if (data.Length == 32)
            {
                UInt256 hash = new UInt256(data);
                if (Blockchain.Default != null)
                    block = Blockchain.Default.GetBlock(hash);
                else if (hash == Blockchain.GenesisBlock.Hash)
                    block = Blockchain.GenesisBlock;
                else
                    block = null;
            }
            else
            {
                return false;
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(block));
            return true;
        }

        protected virtual bool Blockchain_GetTransaction(ExecutionEngine engine)
        {
            byte[] hash = engine.EvaluationStack.Pop().GetByteArray();
            Transaction tx = Blockchain.Default?.GetTransaction(new UInt256(hash));
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        protected virtual bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            byte[] hash = engine.EvaluationStack.Pop().GetByteArray();
            AccountState account = Blockchain.Default?.GetAccountState(new UInt160(hash));
            engine.EvaluationStack.Push(StackItem.FromInterface(account));
            return true;
        }

        protected virtual bool Blockchain_GetValidators(ExecutionEngine engine)
        {
            ECPoint[] validators = Blockchain.Default.GetValidators();
            engine.EvaluationStack.Push(validators.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
            return true;
        }

        protected virtual bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            byte[] hash = engine.EvaluationStack.Pop().GetByteArray();
            AssetState asset = Blockchain.Default?.GetAssetState(new UInt256(hash));
            engine.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        protected virtual bool Blockchain_GetContract(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            ContractState contract = Blockchain.Default.GetContract(hash);
            if (contract == null) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        protected virtual bool Header_GetHash(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Hash.ToArray());
            return true;
        }

        protected virtual bool Header_GetVersion(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Version);
            return true;
        }

        protected virtual bool Header_GetPrevHash(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.PrevHash.ToArray());
            return true;
        }

        protected virtual bool Header_GetMerkleRoot(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.MerkleRoot.ToArray());
            return true;
        }

        protected virtual bool Header_GetTimestamp(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.Timestamp);
            return true;
        }

        protected virtual bool Header_GetConsensusData(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.ConsensusData);
            return true;
        }

        protected virtual bool Header_GetNextConsensus(ExecutionEngine engine)
        {
            BlockBase header = engine.EvaluationStack.Pop().GetInterface<BlockBase>();
            if (header == null) return false;
            engine.EvaluationStack.Push(header.NextConsensus.ToArray());
            return true;
        }

        protected virtual bool Block_GetTransactionCount(ExecutionEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Length);
            return true;
        }

        protected virtual bool Block_GetTransactions(ExecutionEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            if (block == null) return false;
            engine.EvaluationStack.Push(block.Transactions.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected virtual bool Block_GetTransaction(ExecutionEngine engine)
        {
            Block block = engine.EvaluationStack.Pop().GetInterface<Block>();
            int index = (int)engine.EvaluationStack.Pop().GetBigInteger();
            if (block == null) return false;
            if (index < 0 || index >= block.Transactions.Length) return false;
            Transaction tx = block.Transactions[index];
            engine.EvaluationStack.Push(StackItem.FromInterface(tx));
            return true;
        }

        protected virtual bool Transaction_GetHash(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Hash.ToArray());
            return true;
        }

        protected virtual bool Transaction_GetType(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push((int)tx.Type);
            return true;
        }

        protected virtual bool Transaction_GetAttributes(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Attributes.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected virtual bool Transaction_GetInputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected virtual bool Transaction_GetOutputs(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Outputs.Select(p => StackItem.FromInterface(p)).ToArray());
            return true;
        }

        protected virtual bool Transaction_GetReferences(ExecutionEngine engine)
        {
            Transaction tx = engine.EvaluationStack.Pop().GetInterface<Transaction>();
            if (tx == null) return false;
            engine.EvaluationStack.Push(tx.Inputs.Select(p => StackItem.FromInterface(tx.References[p])).ToArray());
            return true;
        }

        protected virtual bool Attribute_GetUsage(ExecutionEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push((int)attr.Usage);
            return true;
        }

        protected virtual bool Attribute_GetData(ExecutionEngine engine)
        {
            TransactionAttribute attr = engine.EvaluationStack.Pop().GetInterface<TransactionAttribute>();
            if (attr == null) return false;
            engine.EvaluationStack.Push(attr.Data);
            return true;
        }

        protected virtual bool Input_GetHash(ExecutionEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push(input.PrevHash.ToArray());
            return true;
        }

        protected virtual bool Input_GetIndex(ExecutionEngine engine)
        {
            CoinReference input = engine.EvaluationStack.Pop().GetInterface<CoinReference>();
            if (input == null) return false;
            engine.EvaluationStack.Push((int)input.PrevIndex);
            return true;
        }

        protected virtual bool Output_GetAssetId(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.AssetId.ToArray());
            return true;
        }

        protected virtual bool Output_GetValue(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.Value.GetData());
            return true;
        }

        protected virtual bool Output_GetScriptHash(ExecutionEngine engine)
        {
            TransactionOutput output = engine.EvaluationStack.Pop().GetInterface<TransactionOutput>();
            if (output == null) return false;
            engine.EvaluationStack.Push(output.ScriptHash.ToArray());
            return true;
        }

        protected virtual bool Account_GetScriptHash(ExecutionEngine engine)
        {
            AccountState account = engine.EvaluationStack.Pop().GetInterface<AccountState>();
            if (account == null) return false;
            engine.EvaluationStack.Push(account.ScriptHash.ToArray());
            return true;
        }

        protected virtual bool Account_GetVotes(ExecutionEngine engine)
        {
            AccountState account = engine.EvaluationStack.Pop().GetInterface<AccountState>();
            if (account == null) return false;
            engine.EvaluationStack.Push(account.Votes.Select(p => (StackItem)p.EncodePoint(true)).ToArray());
            return true;
        }

        protected virtual bool Account_GetBalance(ExecutionEngine engine)
        {
            AccountState account = engine.EvaluationStack.Pop().GetInterface<AccountState>();
            UInt256 asset_id = new UInt256(engine.EvaluationStack.Pop().GetByteArray());
            if (account == null) return false;
            Fixed8 balance = account.Balances.TryGetValue(asset_id, out Fixed8 value) ? value : Fixed8.Zero;
            engine.EvaluationStack.Push(balance.GetData());
            return true;
        }

        protected virtual bool Asset_GetAssetId(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.AssetId.ToArray());
            return true;
        }

        protected virtual bool Asset_GetAssetType(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push((int)asset.AssetType);
            return true;
        }

        protected virtual bool Asset_GetAmount(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Amount.GetData());
            return true;
        }

        protected virtual bool Asset_GetAvailable(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Available.GetData());
            return true;
        }

        protected virtual bool Asset_GetPrecision(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push((int)asset.Precision);
            return true;
        }

        protected virtual bool Asset_GetOwner(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Owner.EncodePoint(true));
            return true;
        }

        protected virtual bool Asset_GetAdmin(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Admin.ToArray());
            return true;
        }

        protected virtual bool Asset_GetIssuer(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            engine.EvaluationStack.Push(asset.Issuer.ToArray());
            return true;
        }

        protected virtual bool Contract_GetScript(ExecutionEngine engine)
        {
            ContractState contract = engine.EvaluationStack.Pop().GetInterface<ContractState>();
            if (contract == null) return false;
            engine.EvaluationStack.Push(contract.Script);
            return true;
        }

        protected virtual bool Storage_GetContext(ExecutionEngine engine)
        {
            engine.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = new UInt160(engine.CurrentContext.ScriptHash)
            }));
            return true;
        }

        protected virtual bool Storage_Get(ExecutionEngine engine)
        {
            StorageContext context = engine.EvaluationStack.Pop().GetInterface<StorageContext>();
            ContractState contract = Blockchain.Default.GetContract(context.ScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            StorageItem item = Blockchain.Default.GetStorageItem(new StorageKey
            {
                ScriptHash = context.ScriptHash,
                Key = key
            });
            engine.EvaluationStack.Push(item?.Value ?? new byte[0]);
            return true;
        }
    }
}
