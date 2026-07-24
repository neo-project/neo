// Copyright (C) 2015-2026 The Neo Project.
//
// UT_JPathTokenType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JPathTokenType
    {
        [TestMethod]
        public void Values_AreSequentialFromZero()
        {
            Assert.AreEqual(0, (byte)JPathTokenType.Root);
            Assert.AreEqual(1, (byte)JPathTokenType.Dot);
            Assert.AreEqual(2, (byte)JPathTokenType.LeftBracket);
            Assert.AreEqual(3, (byte)JPathTokenType.RightBracket);
            Assert.AreEqual(4, (byte)JPathTokenType.Asterisk);
            Assert.AreEqual(5, (byte)JPathTokenType.Comma);
            Assert.AreEqual(6, (byte)JPathTokenType.Colon);
            Assert.AreEqual(7, (byte)JPathTokenType.Identifier);
            Assert.AreEqual(8, (byte)JPathTokenType.String);
            Assert.AreEqual(9, (byte)JPathTokenType.Number);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (JPathTokenType value in Enum.GetValues<JPathTokenType>())
                Assert.IsTrue(Enum.IsDefined(value));
        }
    }
}
