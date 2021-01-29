using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;
using System;
using System.Threading;

namespace Neo.Ledger
{
    public sealed class HeaderCache : IDisposable
    {
        private readonly IndexedQueue<Header> headers = new IndexedQueue<Header>();
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        public Header this[uint index]
        {
            get
            {
                readerWriterLock.EnterReadLock();
                try
                {
                    if (headers.Count == 0) return null;
                    uint firstIndex = headers[0].Index;
                    if (index < firstIndex) return null;
                    index -= firstIndex;
                    if (index >= headers.Count) return null;
                    return headers[(int)index];
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
            }
        }

        public bool Full => headers.Count >= 10000;

        public Header Last
        {
            get
            {
                readerWriterLock.EnterReadLock();
                try
                {
                    if (headers.Count == 0) return null;
                    return headers[^1];
                }
                finally
                {
                    readerWriterLock.ExitReadLock();
                }
            }
        }

        public void Dispose()
        {
            readerWriterLock.Dispose();
        }

        internal void Add(Header header)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                headers.Enqueue(header);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        internal void TryRemoveFirst()
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                headers.TryDequeue(out _);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public Header[] GetSnapshot()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return headers.ToArray();
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }
    }
}
