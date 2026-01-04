// Copyright (C) 2015-2026 The Neo Project.
//
// HeaderCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Ledger;

/// <summary>
/// Used to cache the headers of the blocks that have not been received.
/// </summary>
public sealed class HeaderCache : IDisposable, IEnumerable<Header>
{
    public const int MaxHeaders = 10_000;

    private readonly IndexedQueue<Header> _headers = new();
    private readonly ReaderWriterLockSlim _readerWriterLock = new();

    /// <summary>
    /// Gets the <see cref="Header"/> at the specified index in the cache.
    /// </summary>
    /// <param name="index">The zero-based index of the <see cref="Header"/> to get.</param>
    /// <returns>The <see cref="Header"/> at the specified index in the cache.</returns>
    public Header? this[uint index]
    {
        get
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                if (_headers.Count == 0) return null;
                var firstIndex = _headers[0].Index;
                if (index < firstIndex) return null;
                index -= firstIndex;
                if (index >= _headers.Count) return null;
                return _headers[(int)index];
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets the number of elements in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                return _headers.Count;
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Indicates whether the cache is full.
    /// </summary>
    public bool Full => Count >= MaxHeaders;

    /// <summary>
    /// Gets the last <see cref="Header"/> in the cache. Or <see langword="null"/> if the cache is empty.
    /// </summary>
    public Header? Last
    {
        get
        {
            _readerWriterLock.EnterReadLock();
            try
            {
                if (_headers.Count == 0) return null;
                return _headers[^1];
            }
            finally
            {
                _readerWriterLock.ExitReadLock();
            }
        }
    }

    public void Dispose()
    {
        _readerWriterLock.Dispose();
    }

    internal bool Add(Header header)
    {
        _readerWriterLock.EnterWriteLock();
        try
        {
            // Enforce the cache limit when Full
            if (_headers.Count >= MaxHeaders)
                return false;

            _headers.Enqueue(header);
        }
        finally
        {
            _readerWriterLock.ExitWriteLock();
        }
        return true;
    }

    internal bool TryRemoveFirst([NotNullWhen(true)] out Header? header)
    {
        _readerWriterLock.EnterWriteLock();
        try
        {
            return _headers.TryDequeue(out header);
        }
        finally
        {
            _readerWriterLock.ExitWriteLock();
        }
    }

    public IEnumerator<Header> GetEnumerator()
    {
        _readerWriterLock.EnterReadLock();
        try
        {
            foreach (var header in _headers)
                yield return header;
        }
        finally
        {
            _readerWriterLock.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
