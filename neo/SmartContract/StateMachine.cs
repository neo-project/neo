using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.SmartContract
{
    public class StateMachine : StateReader
    {
        private CloneCache<UInt160, AccountState> accounts;
        private CloneCache<ECPoint, ValidatorState> validators;
        private CloneCache<UInt256, AssetState> assets;
        private CloneCache<UInt160, ContractState> contracts;
        private CloneCache<StorageKey, StorageItem> storages;

        private Dictionary<UInt160, UInt160> contracts_created = new Dictionary<UInt160, UInt160>();
        private List<NotifyEventArgs> notifications = new List<NotifyEventArgs>();

        public IReadOnlyList<NotifyEventArgs> Notifications => notifications;

        public StateMachine(DataCache<UInt160, AccountState> accounts, DataCache<ECPoint, ValidatorState> validators, DataCache<UInt256, AssetState> assets, DataCache<UInt160, ContractState> contracts, DataCache<StorageKey, StorageItem> storages)
        {
            this.accounts = new CloneCache<UInt160, AccountState>(accounts);
            this.validators = new CloneCache<ECPoint, ValidatorState>(validators);
            this.assets = new CloneCache<UInt256, AssetState>(assets);
            this.contracts = new CloneCache<UInt160, ContractState>(contracts);
            this.storages = new CloneCache<StorageKey, StorageItem>(storages);
            Notify += StateMachine_Notify;
            Register("Neo.Account.SetVotes", Account_SetVotes);
            Register("Neo.Validator.Register", Validator_Register);
            Register("Neo.Asset.Create", Asset_Create);
            Register("Neo.Asset.Renew", Asset_Renew);
            Register("Neo.Contract.Create", Contract_Create);
            Register("Neo.Contract.Migrate", Contract_Migrate);
            Register("Neo.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("Neo.Contract.Destroy", Contract_Destroy);
            Register("Neo.Storage.Put", Storage_Put);
            Register("Neo.Storage.Delete", Storage_Delete);
            #region Old AntShares APIs
            Register("AntShares.Account.SetVotes", Account_SetVotes);
            Register("AntShares.Validator.Register", Validator_Register);
            Register("AntShares.Asset.Create", Asset_Create);
            Register("AntShares.Asset.Renew", Asset_Renew);
            Register("AntShares.Contract.Create", Contract_Create);
            Register("AntShares.Contract.Migrate", Contract_Migrate);
            Register("AntShares.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("AntShares.Contract.Destroy", Contract_Destroy);
            Register("AntShares.Storage.Put", Storage_Put);
            Register("AntShares.Storage.Delete", Storage_Delete);
            #endregion
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

        private void StateMachine_Notify(object sender, NotifyEventArgs e)
        {
            notifications.Add(e);
        }

        protected override bool Blockchain_GetAccount(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            engine.EvaluationStack.Push(StackItem.FromInterface(accounts[hash]));
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
            ECPoint[] votes = engine.EvaluationStack.Pop().GetArray().Select(p => ECPoint.DecodePoint(p.GetByteArray(), ECCurve.Secp256r1)).ToArray();
            if (account == null) return false;
            if (votes.Length > 1024) return false;
            account = accounts[account.ScriptHash];
            if (account.IsFrozen) return false;
            if ((!account.Balances.TryGetValue(Blockchain.GoverningToken.Hash, out Fixed8 balance) || balance.Equals(Fixed8.Zero)) && votes.Length > 0)
                return false;
            if (!CheckWitness(engine, account.ScriptHash)) return false;
            account = accounts.GetAndChange(account.ScriptHash);
            account.Votes = votes.Distinct().ToArray();
            return true;
        }

        private bool Validator_Register(ExecutionEngine engine)
        {
            ECPoint pubkey = ECPoint.DecodePoint(engine.EvaluationStack.Pop().GetByteArray(), ECCurve.Secp256r1);
            if (pubkey.IsInfinity) return false;
            if (!CheckWitness(engine, pubkey)) return false;
            ValidatorState validator = validators.GetOrAdd(pubkey, () => new ValidatorState
            {
                PublicKey = pubkey
            });
            engine.EvaluationStack.Push(StackItem.FromInterface(validator));
            return true;
        }

        private bool Asset_Create(ExecutionEngine engine)
        {
            InvocationTransaction tx = (InvocationTransaction)engine.ScriptContainer;
            AssetType asset_type = (AssetType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            if (!Enum.IsDefined(typeof(AssetType), asset_type) || asset_type == AssetType.CreditFlag || asset_type == AssetType.DutyFlag || asset_type == AssetType.GoverningToken || asset_type == AssetType.UtilityToken)
                return false;
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 1024)
                return false;
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            Fixed8 amount = new Fixed8((long)engine.EvaluationStack.Pop().GetBigInteger());
            if (amount == Fixed8.Zero || amount < -Fixed8.Satoshi) return false;
            if (asset_type == AssetType.Invoice && amount != -Fixed8.Satoshi)
                return false;
            byte precision = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            if (precision > 8) return false;
            if (asset_type == AssetType.Share && precision != 0) return false;
            if (amount != -Fixed8.Satoshi && amount.GetData() % (long)Math.Pow(10, 8 - precision) != 0)
                return false;
            ECPoint owner = ECPoint.DecodePoint(engine.EvaluationStack.Pop().GetByteArray(), ECCurve.Secp256r1);
            if (owner.IsInfinity) return false;
            if (!CheckWitness(engine, owner))
                return false;
            UInt160 admin = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 issuer = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
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

        private bool Asset_Renew(ExecutionEngine engine)
        {
            AssetState asset = engine.EvaluationStack.Pop().GetInterface<AssetState>();
            if (asset == null) return false;
            byte years = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            asset = assets.GetAndChange(asset.AssetId);
            if (asset.Expiration < Blockchain.Default.Height + 1)
                asset.Expiration = Blockchain.Default.Height + 1;
            try
            {
                asset.Expiration = checked(asset.Expiration + years * 2000000u);
            }
            catch (OverflowException)
            {
                asset.Expiration = uint.MaxValue;
            }
            engine.EvaluationStack.Push(asset.Expiration);
            return true;
        }

        private bool Contract_Create(ExecutionEngine engine)
        {
            byte[] script = engine.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractParameterType[] parameter_list = engine.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return false;
            ContractParameterType return_type = (ContractParameterType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            bool need_storage = engine.EvaluationStack.Pop().GetBoolean();
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string version = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string author = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string email = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 65536) return false;
            string description = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    HasStorage = need_storage,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                contracts.Add(hash, contract);
                contracts_created.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private bool Contract_Migrate(ExecutionEngine engine)
        {
            byte[] script = engine.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return false;
            ContractParameterType[] parameter_list = engine.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return false;
            ContractParameterType return_type = (ContractParameterType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            bool need_storage = engine.EvaluationStack.Pop().GetBoolean();
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string version = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string author = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return false;
            string email = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 65536) return false;
            string description = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    HasStorage = need_storage,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                contracts.Add(hash, contract);
                contracts_created.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
                if (need_storage)
                {
                    foreach (var pair in storages.Find(engine.CurrentContext.ScriptHash).ToArray())
                    {
                        storages.Add(new StorageKey
                        {
                            ScriptHash = hash,
                            Key = pair.Key.Key
                        }, new StorageItem
                        {
                            Value = pair.Value.Value
                        });
                    }
                }
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return true;
        }

        private bool Contract_GetStorageContext(ExecutionEngine engine)
        {
            ContractState contract = engine.EvaluationStack.Pop().GetInterface<ContractState>();
            if (!contracts_created.TryGetValue(contract.ScriptHash, out UInt160 created)) return false;
            if (!created.Equals(new UInt160(engine.CurrentContext.ScriptHash))) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
            {
                ScriptHash = contract.ScriptHash
            }));
            return true;
        }

        private bool Contract_Destroy(ExecutionEngine engine)
        {
            UInt160 hash = new UInt160(engine.CurrentContext.ScriptHash);
            ContractState contract = contracts.TryGet(hash);
            if (contract == null) return true;
            contracts.Delete(hash);
            if (contract.HasStorage)
                foreach (var pair in storages.Find(hash.ToArray()))
                    storages.Delete(pair.Key);
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
            if (key.Length > 1024) return false;
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
