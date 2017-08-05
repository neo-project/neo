using System;
namespace Neo.Implementations.Blockchains.Utilities
{
    public abstract class AbstractSnapshot : IDisposable
    {
        public abstract void Dispose();
    }
}
