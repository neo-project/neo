using Neo.IO;
using Neo.Ledger;

namespace Neo.Persistence
{
    /// <summary>
    /// Merkle Patricia Tree 
    /// </summary>
    public class MPT
    {
        private static MPTKey GetMPTRootKey(Snapshot snapshot, UInt160 contract) => new MPTKey
        {
            ScriptHash = contract,
            HashKey = snapshot.Contracts[contract].MPTHashRoot
        };

        public static void AddToStorage(Snapshot snapshot, UInt160 contract, StorageKey key, StorageItem item)
        {
            var rootItem = snapshot.MPTStorages[GetMPTRootKey(snapshot, contract)];
            rootItem[key.ToArray()] = item.ToArray();
        }

        public static void DeleteFromStorage(Snapshot snapshot, UInt160 contract, StorageKey key)
        {
            var rootItem = snapshot.MPTStorages[GetMPTRootKey(snapshot, contract)];
            rootItem.Remove(key.ToArray());
        }

        public static StorageItem GetFromStorage(Snapshot snapshot, UInt160 contract, StorageKey key)
        {
            var rootItem = snapshot.MPTStorages[GetMPTRootKey(snapshot, contract)];
            return rootItem[key.ToArray()].ToStorageItem();
        }
    }
}