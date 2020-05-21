using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Tokens
{
    public class Nep11TokenState : IInteroperable
    {
        public byte[] TokenId { set; get; }

        public virtual void FromStackItem(StackItem stackItem)
        {
            TokenId = ((Struct)stackItem)[0].GetSpan().ToArray();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { TokenId };
        }
    }
}
