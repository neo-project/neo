using System.Collections.Generic;
using Neo.VM;

namespace Neo.SmartContract.Iterators
{
    internal class ConcatenatedIterator : IIterator
    {
        private readonly IIterator first, second;
        private IIterator current;

        public ConcatenatedIterator(IIterator first, IIterator second)
        {
            if (second == first)
            {
                var list = new List<StackItem>();

                while (first.Next())
                {
                    list.Add(first.Value());
                }

                var arr = list.ToArray();

                second = new ArrayWrapper(arr);
                first = new ArrayWrapper(arr);
            }

            this.current = this.first = first;
            this.second = second;
        }

        public StackItem Key() => current.Key();
        public StackItem Value() => current.Value();

        public bool Next()
        {
            if (current.Next()) return true;

            current = second;
            return current.Next();
        }


        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
        }
    }
}
