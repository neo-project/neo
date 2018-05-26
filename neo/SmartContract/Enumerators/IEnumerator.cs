using Neo.VM;
using System;

namespace Neo.SmartContract.Enumerators
{
    internal interface IEnumerator : IDisposable, IInteropInterface
    {
        bool Next();
        StackItem Value();
    }
}
