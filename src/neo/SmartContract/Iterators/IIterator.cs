using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Iterators
{
    public interface IIterator : IDisposable
    {
        bool Next();
        StackItem Value();
    }
}
