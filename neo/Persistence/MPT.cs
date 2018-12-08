using Neo.Ledger;

namespace Neo.Persistence
{
    /// <summary>
    /// Merkle Patricia Tree 
    /// </summary>
    public class MPT
    {
        private static MPTKey GetMPTRootKey(Snapshot snapshot, UInt160 contract)
        {
            UInt256 rootHash = snapshot.Contracts[contract].MPTHashRoot;
            return new MPTKey
            {
                ScriptHash = contract,
                HashKey = rootHash
            };
        }

        public static void AddToStorage(Snapshot snapshot, UInt160 contract, StorageKey key, StorageItem item)
        {
            MPTKey rootKey = GetMPTRootKey(snapshot, contract);
            MPTItem rootItem = snapshot.MPTStorages[rootKey];

            // TODO: include on MPT or update!
        }

        public static void DeleteFromStorage(Snapshot snapshot, UInt160 contract, StorageKey key)
        {
            MPTKey rootKey = GetMPTRootKey(snapshot, contract);
            MPTItem rootItem = snapshot.MPTStorages[rootKey];

            // TODO: include on MPT
        }
    }
}