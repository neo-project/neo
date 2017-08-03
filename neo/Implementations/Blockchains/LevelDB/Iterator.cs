using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class Iterator : AbstractIterator
    {
        private IntPtr handle;

        internal Iterator(IntPtr handle)
        {
            this.handle = handle;
        }

        private void CheckError()
        {
            IntPtr error;
            Native.leveldb_iter_get_error(handle, out error);
            NativeHelper.CheckError(error);
        }

        public override void Dispose()
        {
            if (handle != IntPtr.Zero)
            {
                Native.leveldb_iter_destroy(handle);
                handle = IntPtr.Zero;
            }
        }

        public override Slice Key()
        {
            UIntPtr length;
            IntPtr key = Native.leveldb_iter_key(handle, out length);
            CheckError();
            return new Slice(key, length);
        }

        public override void Next()
        {
            Native.leveldb_iter_next(handle);
            CheckError();
        }

        public void Prev()
        {
            Native.leveldb_iter_prev(handle);
            CheckError();
        }

        public override void Seek(Slice target)
        {
            Native.leveldb_iter_seek(handle, target.buffer, (UIntPtr)target.buffer.Length);
        }

        public override void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(handle);
        }

        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(handle);
        }

        public override bool Valid()
        {
            return Native.leveldb_iter_valid(handle);
        }

        public override Slice Value()
        {
            UIntPtr length;
            IntPtr value = Native.leveldb_iter_value(handle, out length);
            CheckError();
            return new Slice(value, length);
        }
    }
}
