using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    // Merkle Patricia Tree
    public class MPT
    {
        private static MPTKey GetMPTRootKey(Snapshot snapshot, UInt160 contract)
        {
            UInt256 rootHash = snapshot[contract].MPTHashRoot;
            return new MPTKey  {
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
