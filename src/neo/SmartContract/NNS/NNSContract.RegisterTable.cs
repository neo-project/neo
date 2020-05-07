using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
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

            UInt160 manager = new UInt160(args[1].GetSpan());

            StorageKey key = CreateStorageKey(Prefix_tokenid, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            if (storage is null) return false;
            DomainState domainInfo = storage.Value.AsSerializable<DomainState>();
            domainInfo.Operator = manager;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = domainInfo.ToArray();
            return true;
        }

        //only can be called by the current owner
        private bool CreateNewDomain(ApplicationEngine engine, string name, UInt160 owner)
        {
            UInt256 nameHash = ComputeNameHash(name);
            DomainState domainInfo = new DomainState { owners = new Dictionary<UInt160, System.Numerics.BigInteger>(), Name = name };
            domainInfo.owners.Add(owner, Factor);
            domainInfo.Operator = owner;
            domainInfo.TimeToLive = engine.Snapshot.Height + 2000000;

            StorageKey token_key = CreateStorageKey(Prefix_tokenid, nameHash);
            StorageItem token_storage = engine.Snapshot.Storages.GetAndChange(token_key, () => new StorageItem()
            {
                Value = domainInfo.ToArray()
            });

            StorageKey owner_key = CreateStorageKey(Prefix_OwnershipMapping, owner);
            StorageItem owner_storage = engine.Snapshot.Storages.GetAndChange(owner_key, () => new StorageItem()
            {
                Value = new SortedSet<UInt256>() { nameHash }.ToByteArray()
            });
            SortedSet<UInt256> domains = new SortedSet<UInt256>(owner_storage.Value.AsSerializableArray<UInt256>());
            domains.Add(nameHash);
            owner_storage.Value = domains.ToByteArray();

            StorageItem storage_totalSupply = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
            storage_totalSupply.Value = (new BigInteger(storage_totalSupply.Value) + BigInteger.One).ToByteArray();
            return true;
        }

        private bool RecoverDomainState(ApplicationEngine engine, string name, UInt160 owner)
        {
            UInt256 nameHash = ComputeNameHash(name);
            StorageKey token_key = CreateStorageKey(Prefix_tokenid, nameHash);
            StorageItem token_storage = engine.Snapshot.Storages.GetAndChange(token_key);
            if (token_storage is null) return false;
            DomainState domainInfo = token_storage.Value.AsSerializable<DomainState>();

            UInt160 oldowner = domainInfo.owners.GetEnumerator().Current.Key;
            StorageKey oldowner_key = CreateStorageKey(Prefix_OwnershipMapping, oldowner);
            StorageItem oldowner_storage = engine.Snapshot.Storages.GetAndChange(oldowner_key);
            if (oldowner_storage is null) return false;
            SortedSet<UInt256> oldowndomains = new SortedSet<UInt256>(oldowner_storage.Value.AsSerializableArray<UInt256>());
            oldowndomains.Remove(nameHash);
            oldowner_storage.Value = oldowndomains.ToByteArray();

            StorageKey adminowner_key = CreateStorageKey(Prefix_OwnershipMapping, owner);
            StorageItem adminowner_storage = engine.Snapshot.Storages.GetAndChange(adminowner_key, () => new StorageItem()
            {
                Value = (new SortedSet<UInt256>()).ToByteArray()
            });
            SortedSet<UInt256> newownerdomains = new SortedSet<UInt256>(adminowner_storage.Value.AsSerializableArray<UInt256>());
            newownerdomains.Add(nameHash);
            adminowner_storage.Value = newownerdomains.ToByteArray();

            domainInfo.owners = new Dictionary<UInt160, BigInteger>();
            domainInfo.owners.Add(owner, Factor);
            domainInfo.Operator = owner;
            domainInfo.TimeToLive = engine.Snapshot.Height + 2000000;
            token_storage.Value = domainInfo.ToArray();
            return true;
        }

        private bool UpdateOwnerShip(ApplicationEngine engine, string name, UInt160 owner, bool isAdded = true)
        {
            UInt256 nameHash = ComputeNameHash(name);
            StorageKey ownerKey = CreateStorageKey(Prefix_OwnershipMapping, owner);
            StorageItem ownerStorage = engine.Snapshot.Storages[ownerKey];
            SortedSet<DomainState> domains = null;
            if (ownerStorage is null) { domains = new SortedSet<DomainState>(); }
            else
            {
                domains = new SortedSet<DomainState>(ownerStorage.Value.AsSerializableArray<DomainState>());
            }

            DomainState domainInfo = GetDomainInfo(engine.Snapshot, nameHash);

            if (isAdded && domains.Add(domainInfo))
            {
                ownerStorage = engine.Snapshot.Storages.GetAndChange(ownerKey);
            }
            else if (!isAdded && domains.Remove(domainInfo))
            {
                ownerStorage = engine.Snapshot.Storages.GetAndChange(ownerKey);
            }
            ownerStorage.Value = domains.ToByteArray();
            return true;
        }
    }
}
