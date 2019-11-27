using Neo.Ledger;
using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class StorageIterator : IIterator
    {
        private readonly IEnumerator<(StorageKey Key, StorageItem Value)> enumerator;

        public StorageIterator(IEnumerator<(StorageKey, StorageItem)> enumerator)
        {
            this.enumerator = enumerator;
        }

        public IIterator Reverse()
        {
            var list = new List<(StorageKey, StorageItem)>();

            while (enumerator.MoveNext())
            {
                list.Insert(0, enumerator.Current);
            }

            return new StorageIterator(list.GetEnumerator());
        }

        public void Dispose()
        {
            enumerator.Dispose();
        }

        public PrimitiveType Key()
        {
            return enumerator.Current.Key.Key;
        }

        public bool Next()
        {
            return enumerator.MoveNext();
        }

        public StackItem Value()
        {
            return enumerator.Current.Value.Value;
        }
    }
}
