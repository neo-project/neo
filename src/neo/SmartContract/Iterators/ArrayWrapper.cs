using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Iterators
{
    internal class ArrayWrapper : IIterator
    {
        private readonly IteratorOrder order;
        private readonly IList<StackItem> array;
        private int index = -1;

        public ArrayWrapper(IList<StackItem> array, IteratorOrder order = IteratorOrder.Ascending)
        {
            this.array = array;
            this.order = order;
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
            if (order == IteratorOrder.Ascending)
            {
                int next = index + 1;
                if (next >= array.Count)
                    return false;
                index = next;
                return true;
            }
            else
            {
                int next = index == -1 ? array.Count - 1 : index - 1;
                if (next < 0)
                    return false;
                index = next;
                return true;
            }
        }

        public IIterator Reverse()
        {
            var ret = new ArrayWrapper(array, order == IteratorOrder.Ascending ? IteratorOrder.Descending : IteratorOrder.Ascending);
            ret.index = index;
            return ret;
        }

        public StackItem Value()
        {
            if (index < 0)
                throw new InvalidOperationException();
            return array[index];
        }
    }
}
