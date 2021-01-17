using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class MapWrapper : IIterator
    {
        private readonly IEnumerator<KeyValuePair<PrimitiveType, StackItem>> enumerator;
        private readonly ReferenceCounter referenceCounter;

        public MapWrapper(IEnumerable<KeyValuePair<PrimitiveType, StackItem>> map, ReferenceCounter referenceCounter)
        {
            this.enumerator = map.GetEnumerator();
            this.referenceCounter = referenceCounter;
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
            return new Struct(referenceCounter) { enumerator.Current.Key, enumerator.Current.Value };
        }
    }
}
