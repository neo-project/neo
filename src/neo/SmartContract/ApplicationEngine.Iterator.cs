using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Iterator_Create = Register("System.Iterator.Create", nameof(CreateIterator), 1 << 4, CallFlags.None);
        public static readonly InteropDescriptor System_Iterator_Next = Register("System.Iterator.Next", nameof(IteratorNext), 1 << 15, CallFlags.None);
        public static readonly InteropDescriptor System_Iterator_Value = Register("System.Iterator.Value", nameof(IteratorValue), 1 << 4, CallFlags.None);

        protected internal IIterator CreateIterator(StackItem item)
        {
            return item switch
            {
                Array array => new ArrayWrapper(array),
                Map map => new MapWrapper(map, ReferenceCounter),
                VM.Types.Buffer buffer => new ByteArrayWrapper(buffer),
                PrimitiveType primitive => new ByteArrayWrapper(primitive),
                _ => throw new ArgumentException()
            };
        }

        protected internal bool IteratorNext(IIterator iterator)
        {
            return iterator.Next();
        }

        protected internal StackItem IteratorValue(IIterator iterator)
        {
            return iterator.Value();
        }
    }
}
