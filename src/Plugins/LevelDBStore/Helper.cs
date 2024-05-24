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

using LevelDB;
using System;
using System.Collections.Generic;

namespace Neo.IO.Data.LevelDB
{
    public static class Helper
    {
        public static IEnumerable<(byte[], byte[])> Seek(this DB db, byte[] prefix, ReadOptions options)
        {
            using var it = db.CreateIterator(options);

            for (it.Seek(prefix); it.IsValid(); it.Next())
                yield return new(it.Key(), it.Value());
        }

        public static IEnumerable<(byte[], byte[])> SeekPrev(this DB db, byte[] prefix, ReadOptions options)
        {
            using var it = db.CreateIterator(options);

            it.Seek(prefix);

            if (!it.IsValid())
                it.SeekToLast();
            else if (it.Key().AsSpan().SequenceCompareTo(prefix) > 0)
                it.Prev();

            for (; it.IsValid(); it.Prev())
                yield return new(it.Key(), it.Value());
        }
    }
}
