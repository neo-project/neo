using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class ArrayWrapper : IIterator
    {
        private readonly IList<StackItem> array;
        private int index = -1;

        public ArrayWrapper(IList<StackItem> array)
        {
            this.array = array;
        }

        public void Dispose()
        {
        }

        public StackItem Key()
        {
            if (index < 0)
                throw new InvalidOperationException();
            return index;
        }

        public bool Next()
        {
            int next = index + 1;
            if (next >= array.Count)
                return false;
            index = next;
            return true;
        }

        public StackItem Value()
        {
            if (index < 0)
                throw new InvalidOperationException();
            return array[index];
        }
    }
}
