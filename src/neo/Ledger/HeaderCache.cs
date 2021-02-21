using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Neo.Ledger
{
    public sealed class HeaderCache : IDisposable, IEnumerable<Header>
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

        public int Count => headers.Count;
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

        internal bool TryRemoveFirst(out Header header)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                return headers.TryDequeue(out header);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public IEnumerator<Header> GetEnumerator()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                foreach (Header header in headers)
                    yield return header;
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
