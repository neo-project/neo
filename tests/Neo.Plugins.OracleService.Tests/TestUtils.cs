// Copyright (C) 2015-2024 The Neo Project.
//
// TestUtils.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace Neo.Plugins
{
    public static class TestUtils
    {
        public static StorageKey CreateStorageKey(this NativeContract contract, byte prefix, ISerializable key)
        {
            var k = new KeyBuilder(contract.Id, prefix);
            if (key != null) k = k.Add(key);
            return k;
        }

        public static StorageKey CreateStorageKey(this NativeContract contract, byte prefix, uint value)
        {
            return new KeyBuilder(contract.Id, prefix).AddBigEndian(value);
        }
    }
}
