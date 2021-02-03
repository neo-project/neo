using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Iterators
{
    internal class ByteArrayWrapper : IIterator
    {
        private readonly byte[] array;
        private int index = -1;

        public ByteArrayWrapper(PrimitiveType value)
        {
            this.array = value.GetSpan().ToArray();
        }

        public ByteArrayWrapper(VM.Types.Buffer value)
        {
            this.array = value.GetSpan().ToArray();
        }

        public void Dispose() { }

        public bool Next()
        {
            int next = index + 1;
            if (next >= array.Length)
                return false;
            index = next;
            return true;
        }

        public StackItem Value()
        {
            if (index < 0)
                throw new InvalidOperationException();
            return new Integer(array[index]);
        }
    }
}
