// Copyright (C) 2015-2024 The Neo Project.
//
// UT_StringExtensdions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_StringExtensions
    {
        [TestMethod]
        public void TestHexToBytes()
        {
            string nullStr = null;
            _ = nullStr.HexToBytes().ToHexString().Should().Be(Array.Empty<byte>().ToHexString());
            string emptyStr = "";
            emptyStr.HexToBytes().ToHexString().Should().Be(Array.Empty<byte>().ToHexString());
            string str1 = "hab";
            Action action = () => str1.HexToBytes();
            action.Should().Throw<FormatException>();
            string str2 = "0102";
            byte[] bytes = str2.HexToBytes();
            bytes.ToHexString().Should().Be(new byte[] { 0x01, 0x02 }.ToHexString());
        }

        [TestMethod]
        public void TestGetVarSizeString()
        {
            int result = "AA".GetVarSize();
            Assert.AreEqual(3, result);
        }
    }
}
