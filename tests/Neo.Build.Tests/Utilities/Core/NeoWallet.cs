// Copyright (C) 2015-2025 The Neo Project.
//
// NeoWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Tests.MSBuild;
using Neo.Build.Utilities.Core;

namespace Neo.Build.Tests.Utilities.Core
{
    [TestClass]
    public sealed class NeoWallet
    {
        // This is need to execute and log MSBuild tasks
        // in "Neo.Build.Utilities.Core" library
        private readonly TestMSBuildEngine _msBuild = new();

        [TestMethod]
        public void TestMethod1()
        {
            // arrange
            var neoWalletTask = new Build.Utilities.Core.NeoWallet()
            {
                BuildEngine = _msBuild
            };

            // act
            var success = neoWalletTask.Execute();

            // assert
            Assert.IsTrue(success);
            Assert.AreEqual(1, _msBuild.ErrorLog.Length);
            Assert.AreEqual("Hello World", _msBuild.ErrorLog[0].Message);
        }
    }
}
