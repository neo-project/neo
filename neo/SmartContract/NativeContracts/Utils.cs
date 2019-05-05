using Neo.Ledger;
using System;

namespace Neo.SmartContract
{
    partial class NeoService
    {
        private static StorageKey CreateStorageKey(UInt160 script_hash, byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = script_hash,
                Key = new byte[sizeof(byte) + key?.Length ?? 0]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }
    }
}
