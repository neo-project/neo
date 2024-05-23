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

using System.IO;

namespace Neo.Cryptography.MPTTrie.Tests
{
    public static class Helper
    {
        private static readonly byte Prefix = 0xf0;

        public static byte[] ToKey(this UInt256 hash)
        {
            byte[] buffer = new byte[UInt256.Length + 1];
            using (MemoryStream ms = new MemoryStream(buffer, true))
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Prefix);
                hash.Serialize(writer);
            }
            return buffer;
        }
    }
}
