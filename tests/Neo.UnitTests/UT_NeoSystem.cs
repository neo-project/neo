// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NeoSystem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NeoSystem
    {
        private NeoSystem neoSystem;

        [TestInitialize]
        public void Setup()
        {
            neoSystem = TestBlockchain.TheNeoSystem;
        }

        [TestMethod]
        public void TestGetBlockchain() => neoSystem.Blockchain.Should().NotBeNull();

        [TestMethod]
        public void TestGetLocalNode() => neoSystem.LocalNode.Should().NotBeNull();

        [TestMethod]
        public void TestGetTaskManager() => neoSystem.TaskManager.Should().NotBeNull();
    }
}
