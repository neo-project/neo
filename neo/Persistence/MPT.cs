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
        public static void AddToStorage(Snapshot snapshot, UInt160 contract, StorageKey key, StorageItem item)
        {
            // TODO: include on MPT or update!
        }

        public static void DeleteFromStorage(Snapshot snapshot, UInt160 contract, StorageKey key)
        {
            // TODO: include on MPT
        }
    }
}
