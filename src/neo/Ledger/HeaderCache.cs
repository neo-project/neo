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

        public void Add(Header header)
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
                    endPos = (endPos + 1) % max_capacity;
                    if (endPos == startPos)
                    {
                        startPos = (startPos + 1) % max_capacity;
                        startIndex++;
                    }
                }
                headers[endPos] = header;
                endIndex = header.Index;
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
