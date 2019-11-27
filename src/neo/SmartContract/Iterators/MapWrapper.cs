using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class MapWrapper : IIterator
    {
        private readonly IEnumerator<KeyValuePair<PrimitiveType, StackItem>> enumerator;

        public MapWrapper(IEnumerable<KeyValuePair<PrimitiveType, StackItem>> map)
        {
            this.enumerator = map.GetEnumerator();
        }

        public IIterator Reverse()
        {
            var list = new List<KeyValuePair<PrimitiveType, StackItem>>();

            while (enumerator.MoveNext())
            {
                list.Insert(0, enumerator.Current);
            }

            return new MapWrapper(list);
        }

        public void Dispose()
        {
            enumerator.Dispose();
        }

        public PrimitiveType Key()
        {
            return enumerator.Current.Key;
        }

        public bool Next()
        {
            return enumerator.MoveNext();
        }

        public StackItem Value()
        {
            return enumerator.Current.Value;
        }
    }
}
