// Copyright (C) 2015-2024 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Neo.IO.Storage.LevelDB
{
    public static class Helper
    {
        public static IEnumerable<(byte[], byte[])> Seek(this DB db, ReadOptions options, byte[] keyOrPrefix, SeekDirection direction)
        {
            using Iterator it = db.CreateIterator(options);
            if (direction == SeekDirection.Forward)
            {
                for (it.Seek(keyOrPrefix); it.Valid(); it.Next())
                    yield return new(it.Key(), it.Value());
            }
            else
            {
                // SeekForPrev
                it.Seek(keyOrPrefix);
                if (!it.Valid())
                    it.SeekToLast();
                else if (it.Key().AsSpan().SequenceCompareTo(keyOrPrefix) > 0)
                    it.Prev();

                for (; it.Valid(); it.Prev())
                    yield return new(it.Key(), it.Value());
            }
        }

        internal static byte[] ToByteArray(this IntPtr data, UIntPtr length)
        {
            if (data == IntPtr.Zero) return null;
            byte[] buffer = new byte[(int)length];
            Marshal.Copy(data, buffer, 0, (int)length);
            return buffer;
        }
    }
}
