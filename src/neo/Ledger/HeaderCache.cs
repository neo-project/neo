using Neo.Network.P2P.Payloads;
using System;
using System.Threading;

namespace Neo.Ledger
{
    internal class HeaderCache : IDisposable
    {
        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly Header[] headers;
        private readonly int max_capacity;
        private int startPos = -1;
        private int endPos = -1;
        private uint startIndex;
        private uint endIndex;

        public bool Added => startPos != -1;

        public HeaderCache(int max_capacity)
        {
            if (max_capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(max_capacity));
            this.max_capacity = max_capacity;
            this.headers = new Header[max_capacity];
        }

        public void Dispose()
        {
            readerWriterLock.Dispose();
        }

        public bool Add(Header header)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                if (!Added)
                {
                    startIndex = header.Index;
                    startPos = 0;
                    endPos = 0;
                }
                else
                {
                    if (header.Index != headers[endPos].Index + 1) throw new ArgumentException("illegal header");
                    var newEndPos = (endPos + 1) % max_capacity;
                    if (newEndPos == startPos) return false;
                    endPos = newEndPos;
                }
                headers[endPos] = header;
                endIndex = header.Index;
                return true;
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public bool Remove(uint height)
        {
            readerWriterLock.EnterWriteLock();
            try
            {
                if (!Added) return false;
                if (startIndex != height) return false;
                headers[startPos] = null;
                startPos = (startPos + 1) % max_capacity;
                if ((endPos + 1) % max_capacity == startPos)
                {
                    startPos = -1;
                    endPos = -1;
                    return true;
                }
                startIndex = headers[startPos].Index;
                return true;
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public Header At(uint height)
        {
            readerWriterLock.EnterReadLock();
            try
            {
                if (startPos == -1 || height < startIndex || height > endIndex) return null;
                return headers[(startPos + height - startIndex) % max_capacity];
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        public uint HeaderHeight()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return endIndex;
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }

        public Header CurrentHeader()
        {
            readerWriterLock.EnterReadLock();
            try
            {
                return endPos == -1 ? null : headers[endPos];
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
        }
    }
}
