// Copyright (C) 2015-2024 The Neo Project.
//
// Extensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Persistence;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Plugins
{
    public static class Extensions
    {
        public static bool NotNull(this StackItem item)
        {
            return !item.IsNull;
        }

        public static string ToBase64(this ReadOnlySpan<byte> item)
        {
            return item == null ? String.Empty : Convert.ToBase64String(item);
        }

        public static int GetVarSize(this ByteString item)
        {
            var length = item.GetSpan().Length;
            return IO.Helper.GetVarSize(length) + length;
        }

        public static int GetVarSize(this BigInteger item)
        {
            var length = item.GetByteCount();
            return IO.Helper.GetVarSize(length) + length;
        }

        public static IEnumerable<(TKey, TValue)> FindPrefix<TKey, TValue>(this IStore db, byte[] prefix)
            where TKey : ISerializable, new()
            where TValue : class, ISerializable, new()
        {
            foreach (var (key, value) in db.Seek(prefix, SeekDirection.Forward))
            {
                if (!key.AsSpan().StartsWith(prefix)) break;
                yield return (key.AsSerializable<TKey>(1), value.AsSerializable<TValue>());
            }
        }

        public static IEnumerable<(TKey, TValue)> FindRange<TKey, TValue>(this IStore db, byte[] startKey, byte[] endKey)
            where TKey : ISerializable, new()
            where TValue : class, ISerializable, new()
        {
            foreach (var (key, value) in db.Seek(startKey, SeekDirection.Forward))
            {
                if (key.AsSpan().SequenceCompareTo(endKey) > 0) break;
                yield return (key.AsSerializable<TKey>(1), value.AsSerializable<TValue>());
            }
        }
    }
}
