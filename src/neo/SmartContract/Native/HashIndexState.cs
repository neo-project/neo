using Neo.IO;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native
{
    internal class HashIndexState : IInteroperable
    {
        public UInt256 Hash;
        public uint Index;

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Hash = new UInt256(@struct[0].GetSpan());
            Index = (uint)@struct[1].GetInteger();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Hash.ToArray(), Index };
        }
    }
}
