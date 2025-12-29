// Copyright (C) 2015-2025 The Neo Project.
//
// IReadOnlyStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Persistence;

/// <summary>
/// This interface provides methods to read from the database.
/// </summary>
public interface IReadOnlyStore : IReadOnlyStore<StorageKey, StorageItem> { }

/// <summary>
/// This interface provides methods to read from the database.
/// </summary>
public interface IReadOnlyStore<TKey, TValue> where TKey : class?
{
    /// <summary>
    /// Gets the entry with the specified key.
    /// </summary>
    /// <param name="key">The key to get.</param>
    /// <returns>The entry if found, throws a <see cref="KeyNotFoundException"/> otherwise.</returns>
    public TValue this[TKey key]
    {
        get
        {
            return TryGet(key) ?? throw new KeyNotFoundException();
        }
    }

    /// <summary>
    /// Reads a specified entry from the database.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <returns>The data of the entry; <see langword="null"/> if the value is not found in the database.</returns>
    TValue? TryGet(TKey key);

    /// <summary>
    /// Reads a specified entry from the database.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <param name="value">The data of the entry.</param>
    /// <returns><see langword="true"/> if the entry exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        value = TryGet(key);
        return value is not null;
    }

    /// <summary>
    /// Determines whether the database contains the specified entry.
    /// </summary>
    /// <param name="key">The key of the entry.</param>
    /// <returns><see langword="true"/> if the database contains an entry with the specified key; otherwise, <see langword="false"/>.</returns>
    bool Contains(TKey key);

    /// <summary>
    /// Finds the entries starting with the specified prefix.
    /// </summary>
    /// <param name="keyPrefix">The prefix of the key.</param>
    /// <param name="direction">The search direction.</param>
    /// <returns>The entries found with the desired prefix.</returns>
    public IEnumerable<(TKey Key, TValue Value)> Find(TKey? keyPrefix = null, SeekDirection direction = SeekDirection.Forward);

    /// <summary>
    /// Returns an enumerable collection of key/value pairs within the specified key range, ordered according to the
    /// specified seek direction.
    /// </summary>
    /// <param name="start">The inclusive lower bound of the key range to search, represented as a byte array. Cannot be null.</param>
    /// <param name="end">The exclusive upper bound of the key range to search, represented as a byte array. Cannot be null.</param>
    /// <param name="direction">The direction in which to enumerate the results. Use SeekDirection.Forward to enumerate in ascending key order,
    /// or SeekDirection.Backward for descending order. The default is SeekDirection.Forward.</param>
    /// <returns>An enumerable collection of key/value pairs whose keys are greater than or equal to <paramref name="start"/> and
    /// less than <paramref name="end"/>, ordered according to <paramref name="direction"/>. The collection is empty if
    /// no keys are found in the specified range.</returns>
    public IEnumerable<(TKey Key, TValue Value)> FindRange(byte[] start, byte[] end, SeekDirection direction = SeekDirection.Forward);
}
