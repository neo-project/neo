using System;
namespace Neo.Implementations.Blockchains.Utilities
{
    public abstract class AbstractWriteBatch
    {
        public abstract void Clear();

        public abstract void Delete(Slice key);

        public abstract void Put(Slice key, Slice value);
    }
}
