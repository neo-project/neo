// Copyright (C) 2015-2024 The Neo Project.
//
// UT_JBoolean.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JBoolean
    {
        private JBoolean jFalse;
        private JBoolean jTrue;

        [TestInitialize]
        public void SetUp()
        {
            jFalse = new JBoolean();
            jTrue = new JBoolean(true);
        }

        [TestMethod]
        public void TestAsNumber()
        {
            jFalse.AsNumber().Should().Be(0);
            jTrue.AsNumber().Should().Be(1);
        }

        [TestMethod]
        public void TestEqual()
        {
            Assert.IsTrue(jTrue.Equals(new JBoolean(true)));
            Assert.IsTrue(jTrue == new JBoolean(true));
            Assert.IsTrue(jFalse.Equals(new JBoolean()));
            Assert.IsTrue(jFalse == new JBoolean());
        }
    }
}
