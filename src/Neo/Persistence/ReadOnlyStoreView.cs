// Copyright (C) 2015-2024 The Neo Project.
//
// ReadOnlyStoreView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using System.Collections.Generic;

namespace Neo.Persistence
{
    public class ReadOnlyStoreView : IReadOnlyStoreView
    {
        private readonly IReadOnlyStore store;

        public ReadOnlyStoreView(IReadOnlyStore store)
        {
            this.store = store;
        }

        /// <inheritdoc/>
        public bool Contains(StorageKey key) => store.Contains(key.ToArray());

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
        public bool TryGet(StorageKey key, out StorageItem item)
        {
            var ok = store.TryGet(key.ToArray(), out byte[] value);
            item = ok ? new StorageItem(value) : null;
            return ok;
        }
    }
}
