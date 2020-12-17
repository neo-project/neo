using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public class AccountState : IInteroperable
    {
        public BigInteger Balance;

        public virtual void FromStackItem(StackItem stackItem)
        {
            Balance = ((Struct)stackItem)[0].GetInteger();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Balance };
        }
    }
}
