using System;
namespace Neo.Implementations.Blockchains.Utilities
{
    public abstract class AbstractDB : IDisposable
    {
        public abstract void Dispose();

        public abstract AbstractIterator NewIterator(AbstractReadOptions options);

        public abstract Slice Get(AbstractReadOptions options, Slice key);

        public abstract bool TryGet(AbstractReadOptions options, Slice key, out Slice value);

        public abstract void Write(AbstractWriteOptions options, AbstractWriteBatch write_batch);

        public abstract void Put(AbstractWriteOptions options, Slice key, Slice value);

        public abstract AbstractSnapshot GetSnapshot();
    }
}
