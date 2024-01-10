// Copyright (C) 2015-2024 The Neo Project.
//
// UtUnsafe.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtUnsafe
    {
        [TestMethod]
        public void NotZero()
        {
            Assert.IsFalse(Unsafe.NotZero(System.Array.Empty<byte>()));
            Assert.IsFalse(Unsafe.NotZero(new byte[4]));
            Assert.IsFalse(Unsafe.NotZero(new byte[8]));
            Assert.IsFalse(Unsafe.NotZero(new byte[11]));

            Assert.IsTrue(Unsafe.NotZero(new byte[4] { 0x00, 0x00, 0x00, 0x01 }));
            Assert.IsTrue(Unsafe.NotZero(new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }));
            Assert.IsTrue(Unsafe.NotZero(new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }));
        }
    }
}
