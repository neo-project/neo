// Copyright (C) 2015-2024 The Neo Project.
//
// LevelDBIterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace StateBench;

using Neo.Persistence;
using System;
using System.Collections.Generic;

public class LevelDbIterator(IReadOnlyStore store)
{
    public IEnumerable<(byte[] Key, byte[] Value)> IterateAllKeyValuePairs()
    {
        // Start from the beginning of the database
        byte[] startKey = [];

        // Iterate in forward direction
        foreach (var (key, value) in store.Seek(startKey, SeekDirection.Forward))
        {
            yield return (key, value);
        }
    }

    public void PrintAllKeyValuePairs()
    {
        foreach (var (key, value) in IterateAllKeyValuePairs())
        {
            Console.WriteLine($"Key: {BitConverter.ToString(key)}, Value: {BitConverter.ToString(value)}");
        }
    }
}
