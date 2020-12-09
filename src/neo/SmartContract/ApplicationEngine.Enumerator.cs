using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Enumerator_Create = Register("System.Enumerator.Create", nameof(CreateEnumerator), 0_00000016, CallFlags.None, false);
        public static readonly InteropDescriptor System_Enumerator_Next = Register("System.Enumerator.Next", nameof(EnumeratorNext), 0_00032768, CallFlags.None, false);
        public static readonly InteropDescriptor System_Enumerator_Value = Register("System.Enumerator.Value", nameof(EnumeratorValue), 0_00000016, CallFlags.None, false);
        public static readonly InteropDescriptor System_Enumerator_Concat = Register("System.Enumerator.Concat", nameof(ConcatEnumerators), 0_00000016, CallFlags.None, false);

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

        protected internal IEnumerator ConcatEnumerators(IEnumerator first, IEnumerator second)
        {
            return new ConcatenatedEnumerator(first, second);
        }
    }
}
