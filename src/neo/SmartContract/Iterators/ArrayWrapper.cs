using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class ArrayWrapper<T> : IIterator
    {
        private readonly IReadOnlyList<T> array;
        private readonly Func<T, StackItem> conversion;

        private int index = -1;

        public ArrayWrapper(IReadOnlyList<T> array, Func<T, StackItem> conversion)
        {
            this.array = array;
            this.conversion = conversion;
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
            return conversion(array[index]);
        }
    }

    internal class ArrayWrapper : ArrayWrapper<StackItem>
    {
        public ArrayWrapper(IReadOnlyList<StackItem> array) : base(array, (a) => a) { }
    }
}
