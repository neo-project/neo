using Neo.VM;
using System;

namespace Neo.SmartContract
{
    internal abstract class Iterator : IDisposable, IInteropInterface
    {
        public abstract void Dispose();
        public abstract StackItem Key();
        public abstract bool Next();
        public abstract StackItem Value();
    }
}
