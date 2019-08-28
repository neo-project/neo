using System;

namespace Neo.IO.Data.LevelDB
{
    public class Iterator : IDisposable
    {
        private IntPtr handle;

        internal Iterator(IntPtr handle)
        {
            this.handle = handle;
        }

        private void CheckError()
        {
            Native.leveldb_iter_get_error(handle, out var error);
            NativeHelper.CheckError(error);
        }

        public void Dispose()
        {
            if (handle != IntPtr.Zero)
            {
                Native.leveldb_iter_destroy(handle);
                handle = IntPtr.Zero;
            }
        }

        public Slice Key()
        {
            IntPtr key = Native.leveldb_iter_key(handle, out var length);
            CheckError();
            return new Slice(key, length);
        }

        public void Next()
        {
            Native.leveldb_iter_next(handle);
            CheckError();
        }

        public void Prev()
        {
            Native.leveldb_iter_prev(handle);
            CheckError();
        }

        public void Seek(Slice target)
        {
            Native.leveldb_iter_seek(handle, target.buffer, (UIntPtr)target.buffer.Length);
        }

        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(handle);
        }

        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(handle);
        }

        public bool Valid()
        {
            return Native.leveldb_iter_valid(handle);
        }

        public Slice Value()
        {
            IntPtr value = Native.leveldb_iter_value(handle, out var length);
            CheckError();
            return new Slice(value, length);
        }
    }
}
