using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        //set the admin of the name, only can called by the current owner
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "manager" })]
        private StackItem SetManager(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            if (isExpired(engine.Snapshot, nameHash)) return false;

            UInt160 manager = new UInt160(args[1].GetSpan());

            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            if (storage is null) return false;
            DomainInfo domainInfo = storage.Value.AsSerializable<DomainInfo>();
            domainInfo.Manager = manager;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = domainInfo.ToArray();
            return true;
        }

        //only can be called by the current owner
        private bool SetOwner(ApplicationEngine engine, string name, UInt160 owner)
        {
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            DomainInfo domainInfo = new DomainInfo { Owner = owner, Name = name };
            if (storage is null)
            {
                engine.Snapshot.Storages.Add(key, new StorageItem
                {
                    Value = domainInfo.ToArray()
                });
                return true;
            }
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = domainInfo.ToArray();
            return true;
        }

        private bool UpdateOwnerShip(ApplicationEngine engine, string name, UInt160 owner, bool isAdded = true)
        {
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey ownerKey = CreateStorageKey(Prefix_OwnershipMapping, owner);
            StorageItem ownerStorage = engine.Snapshot.Storages[ownerKey];
            SortedSet<DomainInfo> domains = null;
            if (ownerStorage is null) { domains = new SortedSet<DomainInfo>(); }
            else
            {
                domains = new SortedSet<DomainInfo>(ownerStorage.Value.AsSerializableArray<DomainInfo>());
            }

            DomainInfo domainInfo = GetDomainInfo(engine.Snapshot, nameHash);

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
