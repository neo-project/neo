// Copyright (C) 2015-2025 The Neo Project.
//
// IStoreEvents.cs file belongs to the neo project and is free
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
    // Write
    public delegate void OnPutDelegate<TKey, TValue>(TKey key, TValue value);
    public delegate void OnDeleteDelegate<TKey>(TKey key);

    // Read
    public delegate void OnTryGetDelegate<TKey>(TKey key);
    public delegate void OnContainsDelegate<TKey>(TKey key);
    public delegate void OnFindDelegate<TKey>(TKey? keyOrPrefix, SeekDirection direction);

    public interface IStoreEvents<TKey, TValue>
        where TKey : class?
    {
        public event OnPutDelegate<TKey, TValue> OnPut;
        public event OnDeleteDelegate<TKey> OnDelete;
        public event OnTryGetDelegate<TKey> OnTryGet;
        public event OnContainsDelegate<TKey> OnContains;
        public event OnFindDelegate<TKey> OnFind;
    }
}

#nullable disable
