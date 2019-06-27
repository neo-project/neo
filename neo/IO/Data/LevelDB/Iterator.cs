using System;

namespace Neo.IO.Data.LevelDB
{
    public class Iterator : IDisposable
    {
        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => Handle == IntPtr.Zero;

        /// <summary>
        /// Handle
        /// </summary>
        internal IntPtr Handle { get; private set; }

        internal Iterator(IntPtr handle)
        {
            Handle = handle;
        }

        private void CheckError()
        {
            IntPtr error;
            Native.leveldb_iter_get_error(Handle, out error);
            NativeHelper.CheckError(error);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_iter_destroy(Handle);
                Handle = IntPtr.Zero;
            }
        }

        public Slice Key()
        {
            UIntPtr length;
            IntPtr key = Native.leveldb_iter_key(Handle, out length);
            CheckError();
            return new Slice(key, length);
        }

        public void Next()
        {
            Native.leveldb_iter_next(Handle);
            CheckError();
        }

        public void Prev()
        {
            Native.leveldb_iter_prev(Handle);
            CheckError();
        }

        public void Seek(Slice target)
        {
            Native.leveldb_iter_seek(Handle, target.buffer, (UIntPtr)target.buffer.Length);
        }

        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(Handle);
        }

        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(Handle);
        }

        public bool Valid()
        {
            return Native.leveldb_iter_valid(Handle);
        }

        public Slice Value()
        {
            UIntPtr length;
            IntPtr value = Native.leveldb_iter_value(Handle, out length);
            CheckError();
            return new Slice(value, length);
        }
    }
}
