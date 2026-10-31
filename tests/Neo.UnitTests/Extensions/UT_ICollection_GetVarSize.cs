// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ICollection_GetVarSize.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;
using System.Runtime.InteropServices;

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_ICollection_GetVarSize
    {
        [TestMethod]
        public void GetVarSize_ISerializable_IncludesElementSizes()
        {
            UInt160[] hashes =
            [
                UInt160.Zero,
                UInt160.Parse("0x0000000000000000000000000000000000000001")
            ];
            var size = hashes.GetVarSize();
            Assert.AreEqual(1 + hashes[0].Size + hashes[1].Size, size);
        }

        [TestMethod]
        public void GetVarSize_Enum_UsesUnderlyingSize()
        {
            CallFlags[] flags = [CallFlags.None, CallFlags.All];
            var size = flags.GetVarSize();
            Assert.AreEqual(1 + flags.Length * sizeof(byte), size);
        }

        [TestMethod]
        public void GetVarSize_ValueType_UsesMarshalSize()
        {
            int[] values = [1, 2, 3];
            var size = values.GetVarSize();
            Assert.AreEqual(1 + values.Length * Marshal.SizeOf<int>(), size);
        }

        [TestMethod]
        public void ToByteArray_SerializesCollection()
        {
            UInt160[] hashes = [UInt160.Zero];
            var bytes = hashes.ToByteArray();
            Assert.IsTrue(bytes.Length > 0);
            Assert.AreEqual(hashes.GetVarSize(), bytes.Length);
        }
    }
}
