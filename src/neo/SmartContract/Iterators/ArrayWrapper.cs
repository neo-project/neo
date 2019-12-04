using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class ArrayWrapper : IIterator
    {
        private readonly IReadOnlyList<StackItem> array;
        private int index = -1;

        public ArrayWrapper(IReadOnlyList<StackItem> array)
        {
            this.array = array;
        }

        public void Dispose()
        {
        }

        public PrimitiveType Key()
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
