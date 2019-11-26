using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Iterators
{
    internal class ByteArrayWrapper : IIterator
    {
        private readonly byte[] value;
        private int index = -1;

        public ByteArrayWrapper(PrimitiveType array)
        {
            this.value = array.GetSpan().ToArray();
        }

        public void Dispose() { }

        public PrimitiveType Key()
        {
            if (index < 0)
                throw new InvalidOperationException();
            return index;
        }

        public bool Next()
        {
            int next = index + 1;
            if (next >= value.Length)
                return false;
            index = next;
            return true;
        }

        public StackItem Value()
        {
            if (index < 0)
                throw new InvalidOperationException();
            return new Integer(value[index]);
        }
    }
}
