// Copyright (C) 2015-2025 The Neo Project.
//
// ReadOnlyViewExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    public static class ReadOnlyViewExtensions
    {
        /// <summary>
        /// Scans the entries starting with the specified prefix.
        /// <para>
        /// If <paramref name="direction"/> is <see cref="SeekDirection.Forward"/>,
        /// it seeks to the first entry if <paramref name="keyPrefix"/> is null or empty.
        /// </para>
        /// <para>
        /// If <paramref name="direction"/> is <see cref="SeekDirection.Backward"/>,
        /// the <paramref name="keyPrefix"/> cannot be null or empty.
        /// </para>
        /// <para>
        /// If want to scan all entries with <see cref="SeekDirection.Backward"/>,
        /// set <paramref name="keyPrefix"/> to be N * 0xff, where N is the max length of the key.
        /// See <see cref="ArrayExtensions.Repeat"/>.
        /// </para>
        /// </summary>
        /// <param name="view">The view to scan.</param>
        /// <param name="keyPrefix">The prefix of the key.</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>The entries found with the desired prefix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(StorageKey Key, StorageItem Value)> ScanPrefix(
            this IReadOnlyStoreView view,
            byte[]? keyPrefix,
            SeekDirection direction = SeekDirection.Forward)
        {
            var seekPrefix = direction == SeekDirection.Forward ? keyPrefix : keyPrefix.GetSeekPrefix();
            return view.ScanPrefix(keyPrefix, seekPrefix, direction);
        }

        internal static IEnumerable<(StorageKey Key, StorageItem Value)> ScanPrefix(
            this IReadOnlyStoreView view,
            byte[]? keyPrefix,
            byte[]? seekPrefix,
            SeekDirection direction = SeekDirection.Forward)
        {
            foreach (var (key, value) in view.Seek(seekPrefix, direction))
            {
                if (keyPrefix == null || key.ToArray().AsSpan().StartsWith(keyPrefix))
                    yield return new(key, value);
                else if (direction == SeekDirection.Forward || (seekPrefix == null || !key.ToArray().SequenceEqual(seekPrefix)))
                    yield break;
            }
        }

        /// <summary>
        /// Scans the entries in the specified range.
        /// <para>
        /// If <paramref name="direction"/> is <see cref="SeekDirection.Forward"/>,
        /// it seeks to the first entry if <paramref name="inclusiveStartKey"/> is null or empty.
        /// </para>
        /// <para>
        /// If <paramref name="direction"/> is <see cref="SeekDirection.Backward"/>,
        /// the <paramref name="inclusiveStartKey"/> cannot be null or empty.
        /// </para>
        /// <para>
        /// If want to scan all entries with <see cref="SeekDirection.Backward"/>,
        /// set <paramref name="inclusiveStartKey"/> to be N * 0xff and <paramref name="exclusiveEndKey"/> to be empty,
        /// where N is the max length of the key.
        /// </para>
        /// </summary>
        /// <param name="view">The view to scan.</param>
        /// <param name="inclusiveStartKey">The inclusive start key.</param>
        /// <param name="exclusiveEndKey">The exclusive end key.</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>The entries found in the specified range.</returns>
        public static IEnumerable<(StorageKey Key, StorageItem Value)> ScanRange(
            this IReadOnlyStoreView view,
            byte[]? inclusiveStartKey,
            byte[] exclusiveEndKey,
            SeekDirection direction = SeekDirection.Forward)
        {
            ByteArrayComparer comparer = direction == SeekDirection.Forward
                ? ByteArrayComparer.Default
                : ByteArrayComparer.Reverse;
            foreach (var (key, value) in view.Seek(inclusiveStartKey, direction))
            {
                if (comparer.Compare(key.ToArray(), exclusiveEndKey) < 0)
                    yield return new(key, value);
                else
                    yield break;
            }
        }

        /// <summary>
        /// Gets the seek prefix for the specified key prefix.
        /// <para>
        /// If the <paramref name="keyPrefix"/> is all 0xff, and <paramref name="maxSizeWhenAll0xff"/> > 0,
        /// the seek prefix will be set to be byte[maxSizeWhenAll0xff] and filled with 0xff.
        /// </para>
        /// <para>
        /// If the <paramref name="keyPrefix"/> is all 0xff and <paramref name="maxSizeWhenAll0xff"/> is less than or equal to 0,
        /// an ArgumentException will be thrown.
        /// </para>
        /// </summary>
        /// <param name="keyPrefix">The key prefix.</param>
        /// <param name="maxSizeWhenAll0xff">The maximum size when all bytes are 0xff.</param>
        /// <returns>The seek prefix.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="keyPrefix"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="keyPrefix"/> is empty.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="keyPrefix"/> is all 0xff and <paramref name="maxSizeWhenAll0xff"/> is less than or equal to 0.
        /// </exception>
        internal static byte[] GetSeekPrefix(this byte[]? keyPrefix, ushort maxSizeWhenAll0xff = 4096 /* make it long enough */)
        {
            if (keyPrefix == null) // Backwards seek for null prefix is not supported for now.
                throw new ArgumentNullException(nameof(keyPrefix));

            if (keyPrefix.Length == 0) // Backwards seek for zero prefix is not supported for now.
                throw new ArgumentOutOfRangeException(nameof(keyPrefix));

            byte[]? seekPrefix = null;
            for (var i = keyPrefix.Length - 1; i >= 0; i--)
            {
                if (keyPrefix[i] < 0xff)
                {
                    seekPrefix = keyPrefix.Take(i + 1).ToArray();
                    seekPrefix[i]++; // The next key after the key_prefix.
                    break;
                }
            }

            if (seekPrefix == null)
            {
                if (maxSizeWhenAll0xff > 0)
                    seekPrefix = ((byte)0xff).Repeat(maxSizeWhenAll0xff);
                else
                    throw new NotSupportedException("Array filled with max value (0xFF)", new ArgumentException(nameof(keyPrefix)));
            }
            return seekPrefix;
        }
    }
}
