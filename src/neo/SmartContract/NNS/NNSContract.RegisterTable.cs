using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        //只有当前owner可以调用
        private bool SetOwner(ApplicationEngine engine, string name, UInt160 owner)
        {
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            DomainInfo domainInfo = storage.Value.AsSerializable<DomainInfo>();
            if (domainInfo is null) return false;
            domainInfo.Owner = owner;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = domainInfo.ToArray();
            return true;
        }

        private bool UpdateOwnerShip(ApplicationEngine engine, string name, UInt160 owner, bool isAdded = true)
        {
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey ownerKey = CreateStorageKey(Prefix_OwnershipMapping, owner);
            StorageItem ownerStorage = engine.Snapshot.Storages[ownerKey];
            SortedSet<UInt256> nameHashes = null;
            if (ownerStorage is null) { nameHashes = new SortedSet<UInt256>(); }
            else
            {
                nameHashes = new SortedSet<UInt256>(ownerStorage.Value.AsSerializableArray<UInt256>());
            }

            if (isAdded && nameHashes.Add(nameHash))
            {
                ownerStorage = engine.Snapshot.Storages.GetAndChange(ownerKey);
                ownerStorage.Value = nameHashes.ToByteArray();
            }
            else if(!isAdded && nameHashes.Remove(nameHash))
            {
                ownerStorage = engine.Snapshot.Storages.GetAndChange(ownerKey);
                ownerStorage.Value = nameHashes.ToByteArray();
            }
            return true;
        }

        //设置当前域名的admin，只有当前owner可以调用
        private bool SetAdmin(ApplicationEngine engine, string name, UInt160 admin)
        {
            UInt256 nameHash = new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
            StorageKey key = CreateStorageKey(Prefix_Domain, nameHash);
            StorageItem storage = engine.Snapshot.Storages[key];
            DomainInfo domainInfo = storage.Value.AsSerializable<DomainInfo>();
            if (domainInfo is null) return false;
            domainInfo.Admin = admin;
            storage = engine.Snapshot.Storages.GetAndChange(key);
            storage.Value = domainInfo.ToArray();
            return true;
        }
    }
}
