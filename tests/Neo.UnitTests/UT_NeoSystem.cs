// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoSystem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NeoSystem
    {
        private NeoSystem _system;

        [TestInitialize]
        public void Setup()
        {
            _system = TestBlockchain.GetSystem();
        }

        [TestMethod]
        public void TestGetBlockchain() => Assert.IsNotNull(_system.Blockchain);

        [TestMethod]
        public void TestGetLocalNode() => Assert.IsNotNull(_system.LocalNode);

        [TestMethod]
        public void TestGetTaskManager() => Assert.IsNotNull(_system.TaskManager);
    }
}
