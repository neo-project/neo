using Neo.VM.Types;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class ConcatenatedIterator : IIterator
    {
        private readonly IIterator first, second;
        private IIterator current;

        public ConcatenatedIterator(IIterator first, IIterator second)
        {
            this.current = this.first = first;
            this.second = second;
        }

        public PrimitiveType Key() => current.Key();
        public StackItem Value() => current.Value();

        public bool Next()
        {
            if (current.Next()) return true;

            current = second;
            return current.Next();
        }

        public IIterator Reverse()
        {
            var list = new List<KeyValuePair<PrimitiveType, StackItem>>();

            while (Next())
            {
                list.Insert(0, new KeyValuePair<PrimitiveType, StackItem>(Key(), Value()));
            }

            return new MapWrapper(list);
        }

        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
        }
    }
}
