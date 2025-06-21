// Copyright (C) 2015-2025 The Neo Project.
//
// UT_FunctionFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Factories;

namespace Neo.Build.Core.Tests.Factories
{
    [TestClass]
    public class UT_FunctionFactory
    {
        [TestMethod]
        public void GetDevNetworks()
        {
            var expectedNetworkDev0 = 0x30564544u; // DEV0 Magic Code
            var expectedNetworkDev1 = 0x31564544u; // DEV1 Magic Code
            var expectedNetworkDev2 = 0x32564544u; // DEV2 Magic Code
            var expectedNetworkDev3 = 0x33564544u; // DEV3 Magic Code
            var expectedNetworkDev4 = 0x34564544u; // DEV4 Magic Code
            var expectedNetworkDev5 = 0x35564544u; // DEV5 Magic Code
            var expectedNetworkDev6 = 0x36564544u; // DEV6 Magic Code
            var expectedNetworkDev7 = 0x37564544u; // DEV7 Magic Code
            var expectedNetworkDev8 = 0x38564544u; // DEV8 Magic Code
            var expectedNetworkDev9 = 0x39564544u; // DEV9 Magic Code

            Assert.AreEqual(expectedNetworkDev0, FunctionFactory.GetDevNetwork(0));
            Assert.AreEqual(expectedNetworkDev1, FunctionFactory.GetDevNetwork(1));
            Assert.AreEqual(expectedNetworkDev2, FunctionFactory.GetDevNetwork(2));
            Assert.AreEqual(expectedNetworkDev3, FunctionFactory.GetDevNetwork(3));
            Assert.AreEqual(expectedNetworkDev4, FunctionFactory.GetDevNetwork(4));
            Assert.AreEqual(expectedNetworkDev5, FunctionFactory.GetDevNetwork(5));
            Assert.AreEqual(expectedNetworkDev6, FunctionFactory.GetDevNetwork(6));
            Assert.AreEqual(expectedNetworkDev7, FunctionFactory.GetDevNetwork(7));
            Assert.AreEqual(expectedNetworkDev8, FunctionFactory.GetDevNetwork(8));
            Assert.AreEqual(expectedNetworkDev9, FunctionFactory.GetDevNetwork(9));
        }
    }
}
