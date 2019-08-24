using Neo.VM;
using System;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    internal class ContractMethodMetadata
    {
        public Func<ApplicationEngine, VMArray, StackItem> Delegate;
        public long Price;
    }
}
