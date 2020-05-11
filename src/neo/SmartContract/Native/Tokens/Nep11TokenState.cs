using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep11TokenState : IInteroperable
    {
        public string Name { set; get; }
        public abstract void FromStackItem(StackItem stackItem);
        public abstract StackItem ToStackItem(ReferenceCounter referenceCounter);
    }
}
