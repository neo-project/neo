// Copyright (C) 2015-2025 The Neo Project.
//
// IWriteStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface IWriteStore<TKey, TValue>
    {
        /// <summary>
        /// Deletes an entry from the snapshot.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        void Delete(TKey key);

        /// <summary>
        /// Puts an entry to the snapshot.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        void Put(TKey key, TValue value);

        /// <summary>
        /// Puts an entry to the database synchronously.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        void PutSync(TKey key, TValue value) => Put(key, value);
    }
}
