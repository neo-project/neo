using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.IO.Caching;
using AntShares.VM;
using System.Collections.Generic;
using System.Linq;

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
            Register("AntShares.Account.SetVotes", Account_SetVotes);
            Register("AntShares.Storage.Get", Storage_Get);
            Register("AntShares.Storage.Put", Storage_Put);
            Register("AntShares.Storage.Delete", Storage_Delete);
        }

        private UInt160 CheckStorageContext(ExecutionEngine engine, StorageContext context)
        {
            byte[] hash = null;
            switch (context)
            {
                case StorageContext.Current:
                    hash = engine.CurrentContext.ScriptHash;
                    break;
                case StorageContext.CallingContract:
                    hash = engine.CallingContext?.ScriptHash;
                    break;
                case StorageContext.EntryContract:
                    hash = engine.EntryContext.ScriptHash;
                    break;
            }
            if (hash == null) return null;
            UInt160 script_hash = new UInt160(hash);
            ContractState contract = contracts.TryGet(script_hash);
            if (contract == null) return null;
            if (!contract.HasStorage) return null;
            return script_hash;
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

        protected override bool Blockchain_GetAsset(ExecutionEngine engine)
        {
            UInt256 hash = new UInt256(engine.EvaluationStack.Pop().GetByteArray());
            AssetState asset = assets.TryGet(hash);
            if (asset == null) return false;
            engine.EvaluationStack.Push(StackItem.FromInterface(asset));
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

        private bool Storage_Get(ExecutionEngine engine)
        {
            StorageContext context = (StorageContext)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            UInt160 script_hash = CheckStorageContext(engine, context);
            if (script_hash == null) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            StorageItem item = storages.TryGet(new StorageKey
            {
                ScriptHash = script_hash,
                Key = key
            });
            engine.EvaluationStack.Push(item?.Value ?? new byte[0]);
            return true;
        }

        private bool Storage_Put(ExecutionEngine engine)
        {
            StorageContext context = (StorageContext)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            UInt160 script_hash = CheckStorageContext(engine, context);
            if (script_hash == null) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            byte[] value = engine.EvaluationStack.Pop().GetByteArray();
            storages.GetAndChange(new StorageKey
            {
                ScriptHash = script_hash,
                Key = key
            }, () => new StorageItem()).Value = value;
            return true;
        }

        private bool Storage_Delete(ExecutionEngine engine)
        {
            StorageContext context = (StorageContext)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            UInt160 script_hash = CheckStorageContext(engine, context);
            if (script_hash == null) return false;
            byte[] key = engine.EvaluationStack.Pop().GetByteArray();
            storages.Delete(new StorageKey
            {
                ScriptHash = script_hash,
                Key = key
            });
            return true;
        }
    }
}
