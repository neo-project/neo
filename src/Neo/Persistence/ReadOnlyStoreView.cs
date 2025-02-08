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

namespace Neo.Persistence
{
    public class ReadOnlyStoreView : IReadOnlyStoreView
    {
        private readonly IReadOnlyStore _store;

        public ReadOnlyStoreView(IReadOnlyStore store)
        {
            _store = store;
        }

        /// <inheritdoc/>
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
            if (_store.TryGet(key.ToArray(), out var value))
            {
                item = new StorageItem(value);
                return true;
            }

            item = null;
            return false;
        }
    }
}
