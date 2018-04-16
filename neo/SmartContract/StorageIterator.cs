using Neo.Core;
using Neo.VM;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    internal class StorageIterator : Iterator
    {
        private readonly IEnumerator<KeyValuePair<StorageKey, StorageItem>> enumerator;

        public StorageIterator(IEnumerator<KeyValuePair<StorageKey, StorageItem>> enumerator)
        {
            this.enumerator = enumerator;
        }

        public override void Dispose()
        {
            enumerator.Dispose();
        }

        public override StackItem Key()
        {
            return enumerator.Current.Key.Key;
        }

        public override bool Next()
        {
            return enumerator.MoveNext();
        }

        public override StackItem Value()
        {
            return enumerator.Current.Value.Value;
        }
    }
}
