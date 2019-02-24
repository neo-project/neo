using Neo.IO;
using Neo.Ledger;
using Neo.Ledger.MPT;

namespace Neo.Persistence
{
    /// <summary>
    /// Merkle Patricia Tree 
    /// </summary>
    public static class MPT
    {
        /// <summary>
        /// Get the root key for the MPT.
        /// </summary>
        /// <param name="snapshot">Snapshot.</param>
        /// <param name="contract">The contract for which the root will be retrieved.</param>
        /// <returns></returns>
        private static byte[] GetMPTRootKey(this Snapshot snapshot, UInt160 contract)
        {
            var rootKey = new MPTKey
            {
                ScriptHash = contract,
                HashKey = snapshot.Contracts[contract].MPTHashRoot
            };
            var merklePatriciaNode = snapshot.MPTStorages.TryGet(rootKey);
            return merklePatriciaNode?.Value;
        }

        /// <summary>
        /// Create a data strutcture that delegates the calls to the MPTStorages.
        /// </summary>
        /// <param name="snapshot">Snapshot.</param>
        /// <param name="contract">The contract for which the root will be retrieved.</param>
        /// <returns>The MPT delegate.</returns>
        private static MerklePatricia CreateMPT(this Snapshot snapshot, UInt160 contract) =>
            new MerklePatriciaDataCache(snapshot.MPTStorages, GetMPTRootKey(snapshot, contract));

        /// <summary>
        /// Associate a key-value entry to the database.
        /// </summary>
        /// <param name="snapshot">Snapshot.</param>
        /// <param name="contract">The contract for which the root will be retrieved.</param>
        /// <param name="key"></param>
        /// <param name="item"></param>
        public static void AddToMPTStorage(this Snapshot snapshot, UInt160 contract, StorageKey key, StorageItem item)
        {
            snapshot.CreateMPT(contract)[key.ToArray()] = item.ToArray();
        }

        /// <summary>
        /// Removes a key-value mapping from the database.
        /// </summary>
        /// <param name="snapshot">Snapshot.</param>
        /// <param name="contract">The contract for which the root will be retrieved.</param>
        /// <param name="key">The key to be removed.</param>
        public static void DeleteFromMPTStorage(this Snapshot snapshot, UInt160 contract, StorageKey key)
        {
            snapshot.CreateMPT(contract).Remove(key.ToArray());
        }

        /// <summary>
        /// Gets the value associated to the key. 
        /// </summary>
        /// <param name="snapshot">Snapshot.</param>
        /// <param name="contract">The contract for which the root will be retrieved.</param>
        /// <param name="key">The key to be retrieved.</param>
        /// <returns></returns>
        public static StorageItem GetFromMPTStorage(this Snapshot snapshot, UInt160 contract, StorageKey key)
        {
            return snapshot.CreateMPT(contract)[key.ToArray()].ToStorageItem();
        }
    }
}