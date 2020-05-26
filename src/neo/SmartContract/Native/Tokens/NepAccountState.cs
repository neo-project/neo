using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class NepAccountState : IInteroperable
    {
        public BigInteger Balance;

        public virtual void FromStackItem(StackItem stackItem)
        {
            Balance = ((Struct)stackItem)[0].GetBigInteger();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Balance };
        }
    }
}
