using System;
namespace Neo.Implementations.Blockchains.Utilities
{
    public abstract class AbstractIterator : IDisposable
    {
        public abstract void Dispose();

        public abstract void Seek(Slice target);

        public abstract void Next();

        public abstract bool Valid();

        public abstract Slice Key();

        public abstract Slice Value();

        public abstract void SeekToFirst();
    }
}
