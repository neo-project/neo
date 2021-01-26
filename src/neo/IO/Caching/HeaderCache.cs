using Neo.Network.P2P.Payloads;
using System;
using System.Threading;

namespace Neo.IO.Caching
{
    internal class HeaderCache
    {
        private int max_capacity;
        private int startPos = -1;
        private int endPos = -1;
        private uint startIndex;
        private uint endIndex;
        private Header[] headers = null;
        protected readonly ReaderWriterLockSlim RwSyncRootLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public HeaderCache(int max_capacity)
        {
            if (max_capacity <= 0) throw new ArgumentException("illegal max_capacity");
            this.max_capacity = max_capacity;
            headers = new Header[max_capacity];
        }

        public bool Added => startPos != -1;

        public void Add(Header header)
        {
            try
            {
                RwSyncRootLock.EnterWriteLock();
                if (!Added)
                {
                    startIndex = header.Index;
                    endIndex = header.Index;
                    startPos = 0;
                    endPos = 0;
                    headers[0] = header;
                    return;
                }
                else
                {
                    if (header.Index != headers[endPos].Index + 1) throw new ArgumentException("illegal header");
                    endPos = (endPos + 1) % max_capacity;
                    endIndex++;
                }
                if (endPos == startPos)
                {
                    startPos = (startPos + 1) % max_capacity;
                    startIndex++;
                }
                headers[endPos] = header;
            }
            finally
            {
                RwSyncRootLock.ExitWriteLock();
            }
        }

        public Header At(uint height)
        {
            try
            {
                RwSyncRootLock.EnterReadLock();
                if (startPos == -1 || height < startIndex || height > endIndex) return null;
                return headers[(startPos + height - startIndex) % max_capacity];
            }
            finally
            {
                RwSyncRootLock.ExitReadLock();
            }
        }

        public uint HeaderHeight()
        {
            try
            {
                RwSyncRootLock.EnterReadLock();
                return endIndex;
            }
            finally
            {
                RwSyncRootLock.ExitReadLock();
            }
        }

        public Header CurrentHeader()
        {
            try
            {
                RwSyncRootLock.EnterReadLock();
                return endPos == -1 ? null : headers[endPos];
            }
            finally
            {
                RwSyncRootLock.ExitReadLock();
            }
        }
    }
}
