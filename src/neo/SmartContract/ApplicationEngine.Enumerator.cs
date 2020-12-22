using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Enumerator_Create = Register("System.Enumerator.Create", nameof(CreateEnumerator), 1 << 4, CallFlags.None);
        public static readonly InteropDescriptor System_Enumerator_Next = Register("System.Enumerator.Next", nameof(EnumeratorNext), 1 << 15, CallFlags.None);
        public static readonly InteropDescriptor System_Enumerator_Value = Register("System.Enumerator.Value", nameof(EnumeratorValue), 1 << 4, CallFlags.None);

        protected internal IEnumerator CreateEnumerator(StackItem item)
        {
            return item switch
            {
                Array array => new ArrayWrapper(array),
                VM.Types.Buffer buffer => new ByteArrayWrapper(buffer),
                PrimitiveType primitive => new ByteArrayWrapper(primitive),
                _ => throw new ArgumentException()
            };
        }

        protected internal bool EnumeratorNext(IEnumerator enumerator)
        {
            return enumerator.Next();
        }

        protected internal StackItem EnumeratorValue(IEnumerator enumerator)
        {
            return enumerator.Value();
        }
    }
}
