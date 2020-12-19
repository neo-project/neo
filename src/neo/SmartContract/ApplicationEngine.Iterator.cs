using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Iterator_Create = Register("System.Iterator.Create", nameof(CreateIterator), 1 << 4, CallFlags.None);
        public static readonly InteropDescriptor System_Iterator_Key = Register("System.Iterator.Key", nameof(IteratorKey), 1 << 4, CallFlags.None);
        public static readonly InteropDescriptor System_Iterator_Keys = Register("System.Iterator.Keys", nameof(IteratorKeys), 1 << 4, CallFlags.None);
        public static readonly InteropDescriptor System_Iterator_Values = Register("System.Iterator.Values", nameof(IteratorValues), 1 << 4, CallFlags.None);
        public static readonly InteropDescriptor System_Iterator_Concat = Register("System.Iterator.Concat", nameof(ConcatIterators), 1 << 4, CallFlags.None);

        protected internal IIterator CreateIterator(StackItem item)
        {
            return item switch
            {
                Array array => new ArrayWrapper(array),
                Map map => new MapWrapper(map),
                VM.Types.Buffer buffer => new ByteArrayWrapper(buffer),
                PrimitiveType primitive => new ByteArrayWrapper(primitive),
                _ => throw new ArgumentException()
            };
        }

        protected internal PrimitiveType IteratorKey(IIterator iterator)
        {
            return iterator.Key();
        }

        protected internal IEnumerator IteratorKeys(IIterator iterator)
        {
            return new IteratorKeysWrapper(iterator);
        }

        protected internal IEnumerator IteratorValues(IIterator iterator)
        {
            return new IteratorValuesWrapper(iterator);
        }

        protected internal IIterator ConcatIterators(IIterator first, IIterator second)
        {
            return new ConcatenatedIterator(first, second);
        }
    }
}
