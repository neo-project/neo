using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// The base class of account state for all native tokens.
    /// </summary>
    public class AccountState : IInteroperable
    {
        /// <summary>
        /// The balance of the account.
        /// </summary>
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
