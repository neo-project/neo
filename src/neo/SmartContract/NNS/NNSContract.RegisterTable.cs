using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        //set the operator of the name, only can called by the current owner
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "manager" })]
        private StackItem SetOperator(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = ComputeNameHash(name);
            if (IsExpired(engine.Snapshot, nameHash)) return false;
            IEnumerator enumerator= OwnerOf(engine.Snapshot, nameHash);
            UInt160 owner = null;
            while (enumerator.MoveNext()) {
                owner = (UInt160)enumerator.Current;
            }
            if (!owner.Equals(engine.CallingScriptHash) && (!InteropService.Runtime.CheckWitnessInternal(engine, owner))) return false;
            UInt160 manager = new UInt160(args[1].GetSpan());
            StorageKey key = CreateTokenKey(nameHash);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            if (storage is null) return false;
            DomainState domainInfo = storage.Value.AsSerializable<DomainState>();
            domainInfo.Operator = manager;
            storage.Value = domainInfo.ToArray();
            return true;
        }

        //only can be called by the current owner
        private bool CreateNewDomain(ApplicationEngine engine, string name, UInt160 owner)
        {
            UInt256 nameHash = ComputeNameHash(name);
            DomainState domainInfo = new DomainState { Name = name,Operator=owner, TimeToLive = engine.Snapshot.Height + 2000000 };

            StorageKey token_key = CreateTokenKey(nameHash);
            StorageItem token_storage = engine.Snapshot.Storages.GetAndChange(token_key, () => new StorageItem()
            {
                Value = domainInfo.ToArray()
            });

            StorageKey owner_key = CreateOwner2TokenKey(owner,nameHash);
            StorageItem owner_storage = engine.Snapshot.Storages.GetAndChange(owner_key, () => new StorageItem()
            {
                Value = new Nep11AccountState() {Balance=Factor }.ToArray()
            });
            owner_key = CreateToken2OwnerKey(nameHash, owner);
            owner_storage = engine.Snapshot.Storages.GetAndChange(owner_key, () => new StorageItem()
            {
                Value = new Nep11AccountState() { Balance = Factor }.ToArray()
            });
            Accumulator(engine);
            return true;
        }

        private bool RecoverDomainState(ApplicationEngine engine, UInt256 nameHash, UInt160 owner)
        {
            StorageKey token_key = CreateTokenKey(nameHash);
            StorageItem token_storage = engine.Snapshot.Storages.GetAndChange(token_key);
            if (token_storage is null) return false;

            IEnumerator enumerator=OwnerOf(engine.Snapshot, nameHash);
            while (enumerator.MoveNext()) {
                UInt160 oldowner = (UInt160)enumerator.Current;
                engine.Snapshot.Storages.Delete(CreateToken2OwnerKey(nameHash, oldowner));
                engine.Snapshot.Storages.Delete(CreateOwner2TokenKey(oldowner, nameHash));
            }
            StorageKey adminowner_key = CreateToken2OwnerKey(nameHash, owner);
            StorageItem adminowner_storage = engine.Snapshot.Storages.GetAndChange(adminowner_key, () => new StorageItem()
            {
                Value = new Nep11AccountState() { Balance=Factor}.ToArray()
            });
            adminowner_key = CreateOwner2TokenKey(owner, nameHash);
            adminowner_storage = engine.Snapshot.Storages.GetAndChange(adminowner_key, () => new StorageItem()
            {
                Value = new Nep11AccountState() { Balance = Factor }.ToArray()
            });

            DomainState domainInfo = token_storage.Value.AsSerializable<DomainState>();
            domainInfo.Operator = owner;
            domainInfo.TimeToLive = engine.Snapshot.Height + 2000000;
            token_storage.Value = domainInfo.ToArray();
            return true;
        }
    }
}
