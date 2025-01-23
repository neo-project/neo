// Copyright (C) 2015-2025 The Neo Project.
//
// ReadOnlyStoreView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.SmartContract;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Neo.Persistence
{
    /// <summary>
    /// A read-only view of a store.
    /// No cache and lock in this implementation,
    /// so it is faster in some cases(For example, no repeated reads of the same key).
    /// </summary>
    public class ReadOnlyStoreView : IReadOnlyStoreView
    {
        private readonly IReadOnlyStore _store;

        public ReadOnlyStoreView(IReadOnlyStore store)
        {
            _store = store;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(StorageKey key) => _store.Contains(key.ToArray());

        /// <inheritdoc/>
        public StorageItem this[StorageKey key]
        {
            get
            {
                if (TryGet(key, out var item))
                    return item;
                throw new KeyNotFoundException();
            }
        }

        /// <inheritdoc/>
        public bool TryGet(StorageKey key, [NotNullWhen(true)] out StorageItem? item)
        {
            var ok = _store.TryGet(key.ToArray(), out var value);
            item = ok ? new StorageItem(value) : null;
            return ok;
        }

        /// <inheritdoc/>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Seek(
            byte[]? keyOrPrefix = null, SeekDirection direction = SeekDirection.Forward)
        {
            foreach (var (key, value) in _store.Seek(keyOrPrefix, direction))
                yield return new(new(key), new(value));
        }
    }
}
