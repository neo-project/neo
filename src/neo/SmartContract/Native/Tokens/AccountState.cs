using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class AccountState : IInteroperable
    {
        public BigInteger Balance;

        public virtual void FromStackItem(StackItem stackItem)
        {
            Balance = ((Integer)stackItem).GetInteger();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return Balance;
        }
    }
}
