using Neo.Ledger;
using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.SmartContract.Enumerators
{
    internal class StorageKeyEnumerator : IEnumerator
    {
        private readonly IEnumerator<StorageKey> enumerator;
        private readonly byte removePrefix;

        public StorageKeyEnumerator(IEnumerator<StorageKey> enumerator, byte removePrefix)
        {
            this.enumerator = enumerator;
            this.removePrefix = removePrefix;
        }

        public void Dispose()
        {
            enumerator.Dispose();
        }

        public bool Next()
        {
            return enumerator.MoveNext();
        }

        public StackItem Value()
        {
            byte[] key = enumerator.Current.Key;
            if (removePrefix > 0)
                key = key[removePrefix..];
            return key;
        }
    }
}
