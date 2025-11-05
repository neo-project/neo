// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JBoolean.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Json.Nodes;

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JBoolean
    {
        private JsonValue jFalse;
        private JsonValue jTrue;

        [TestInitialize]
        public void SetUp()
        {
            jFalse = JsonValue.Create(false);
            jTrue = JsonValue.Create(true);
        }

        [TestMethod]
        public void TestConversionToOtherTypes()
        {
            Assert.AreEqual("true", jTrue.ToString());
            Assert.AreEqual("false", jFalse.ToString());
        }

        [TestMethod]
        public void TestEqual()
        {
            Assert.AreEqual(jFalse.ToString(), jFalse.GetValue<bool>().ToString().ToLowerInvariant());
        }
    }
}
