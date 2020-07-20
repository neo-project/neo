using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    public interface IInteroperable
    {
        void FromStackItem(StackItem stackItem);
        StackItem ToStackItem(ReferenceCounter referenceCounter);
    }
}
