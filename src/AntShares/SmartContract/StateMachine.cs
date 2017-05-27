using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.IO.Caching;
using AntShares.VM;
using AntShares.Wallets;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntShares.SmartContract
{
    public class StateMachine : StateReader
    {
        private CloneCache<UInt160, AccountState> accounts;
        private CloneCache<ECPoint, ValidatorState> validators;
        private CloneCache<UInt256, AssetState> assets;
        private CloneCache<UInt160, ContractState> contracts;
        private CloneCache<StorageKey, StorageItem> storages;

        public StateMachine(DataCache<UInt160, AccountState> accounts, DataCache<ECPoint, ValidatorState> validators, DataCache<UInt256, AssetState> assets, DataCache<UInt160, ContractState> contracts, DataCache<StorageKey, StorageItem> storages)
        {
            this.accounts = new CloneCache<UInt160, AccountState>(accounts);
            this.validators = new CloneCache<ECPoint, ValidatorState>(validators);
            this.assets = new CloneCache<UInt256, AssetState>(assets);
            this.contracts = new CloneCache<UInt160, ContractState>(contracts);
            this.storages = new CloneCache<StorageKey, StorageItem>(storages);
            Register("AntShares.Blockchain.CreateAsset", Blockchain_CreateAsset);
            Register("AntShares.Blockchain.CreateContract", Blockchain_CreateContract);
            Register("AntShares.Account.SetVotes", Account_SetVotes);
            Register("AntShares.Asset.Renew", Asset_Renew);
            Register("AntShares.Contract.Destroy", Contract_Destroy);
            Register("AntShares.Storage.Put", Storage_Put);
            Register("AntShares.Storage.Delete", Storage_Delete);
        }

        private bool CheckStorageContext(StorageContext context)
        {
            ContractState contract = contracts.TryGet(context.ScriptHash);
            if (contract == null) return false;
            if (!contract.HasStorage) return false;
            return true;
        }

        public void Commit()
        {
            accounts.Commit();
            validators.Commit();
            assets.Commit();
            contracts.Commit();
            storages.Commit();
        }

        private HashSet<UInt160> _hashes_for_verifying = null;
        private HashSet<UInt160> GetScriptHashesForVerifying(ExecutionEngine engine)
        {
            if (_hashes_for_verifying == null)
            {
                IVerifiable container = (IVerifiable)engine.ScriptContainer;
                _hashes_for_verifying = new HashSet<UInt160>(container.GetScriptHashesForVerifying());
            }
            return _hashes_for_verifying;
        }

        protected override bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            engine.EvaluationStack.Push(StackItem.FromInterface(accounts[hash]));
            return true;
        }

        private bool Blockchain_CreateAsset(ExecutionEngine engine)
        {
            InvocationTransaction tx = (InvocationTransaction)engine.ScriptContainer;
            AssetType asset_type = (AssetType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            Fixed8 amount = new Fixed8((long)engine.EvaluationStack.Pop().GetBigInteger());
            byte precision = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            ECPoint owner = ECPoint.DecodePoint(engine.EvaluationStack.Pop().GetByteArray(), ECCurve.Secp256r1);
            UInt160 admin = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 issuer = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            if (!GetScriptHashesForVerifying(engine).Contains(Contract.CreateSignatureRedeemScript(owner).ToScriptHash())) return false;
            AssetState asset = assets.GetOrAdd(tx.Hash, () => new AssetState
            {
                AssetId = tx.Hash,
                AssetType = asset_type,
                Name = name,
                Amount = amount,
                Available = Fixed8.Zero,
                Precision = precision,
                Fee = Fixed8.Zero,
                FeeAddress = new UInt160(),
                Owner = owner,
                Admin = admin,
                Issuer = issuer,
                Expiration = Blockchain.Default.Height + 1 + 2000000,
                IsFrozen = false
            });
            engine.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        protected override bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            UInt256 hash = new UInt256(engine.EvaluationStack.Pop().GetByteArray());
            AssetState asset = assets.TryGet(hash);
            if (asset == null) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(asset));
            return true;
        }

        private bool Blockchain_CreateContract(ExecutionEngine engine)
        {
            byte[] script = engine.EvaluationStack.Pop().GetByteArray();
            ContractParameterType[] parameter_list = engine.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            ContractParameterType return_type = (ContractParameterType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            bool need_storage = engine.EvaluationStack.Pop().GetBoolean();
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            string version = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            string author = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            string email = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            string description = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            ContractState contract = contracts.GetOrAdd(script.ToScriptHash(), () => new ContractState
            {
                Code = new FunctionCode
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type
                },
                HasStorage = need_storage,
                Name = name,
                CodeVersion = version,
                Author = author,
                Email = email,
                Description = description
            });
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        protected override bool Blockchain_GetContract(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            ContractState contract = contracts.TryGet(hash);
            if (contract == null) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private bool Account_SetVotes(ExecutionEngine engine)
        {
            AccountState account = engine.EvaluationStack.Pop().GetInterface<AccountState>();
            if (account == null) return false;
            account = accounts[account.ScriptHash];
            if (account.IsFrozen) return false;
            if (!account.Balances.ContainsKey(Blockchain.SystemShare.Hash) || account.Balances[Blockchain.SystemShare.Hash].Equals(Fixed8.Zero))
                return false;
            if (!GetScriptHashesForVerifying(engine).Contains(account.ScriptHash)) return false;
            account = accounts.GetAndChange(account.ScriptHash);
            account.Votes = engine.EvaluationStack.Pop().GetArray().Select(p => ECPoint.DecodePoint(p.GetByteArray(), ECCurve.Secp256r1)).ToArray();
            return true;
        }

        private bool Asset_Renew(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            byte years = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            asset = assets.GetAndChange(asset.AssetId);
            if (asset.Expiration < Blockchain.Default.Height + 1)
                asset.Expiration = Blockchain.Default.Height + 1;
            asset.Expiration += years * 2000000u;
            return true;
        }

        private bool Contract_Destroy(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.ScriptHash);
            if (contracts.TryGet(hash) == null) return false;
            contracts.Delete(hash);
            return true;
        }

        protected override bool Storage_Get(ExecutionEngine engine)
        {
            StorageContext context = engine.EvaluationStack.Pop().GetInterface<StorageContext>();
            if (!CheckStorageContext(context)) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            StorageItem item = storages.TryGet(new StorageKey
            {
                ScriptHash = context.ScriptHash,
                Key = key
            });
            engine.EvaluationStack.Push(item?.Value ?? new byte[0]);
            return true;
        }

        private bool Storage_Put(ExecutionEngine engine)
        {
            StorageContext context = engine.EvaluationStack.Pop().GetInterface<StorageContext>();
            if (!CheckStorageContext(context)) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            byte[] value = engine.EvaluationStack.Pop().GetByteArray();
            storages.GetAndChange(new StorageKey
            {
                ScriptHash = context.ScriptHash,
                Key = key
            }, () => new StorageItem()).Value = value;
            return true;
        }

        private bool Storage_Delete(ExecutionEngine engine)
        {
            StorageContext context = engine.EvaluationStack.Pop().GetInterface<StorageContext>();
            if (!CheckStorageContext(context)) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            storages.Delete(new StorageKey
            {
                ScriptHash = context.ScriptHash,
                Key = key
            });
            return true;
        }
    }
}
