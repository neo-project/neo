using Neo.VM;
using System;

namespace Neo.SmartContract.Enumerators
{
    internal interface IEnumerator : IDisposable
    {
        bool Next();
        StackItem Value();
    }
}
