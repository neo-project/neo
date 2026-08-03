// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Treasury.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_Treasury
    {
        [TestMethod]
        public void Activations_RequireFaun()
        {
            Assert.IsTrue(NativeContract.Treasury.Activations.Contains(Hardfork.HF_Faun));
        }

        [TestMethod]
        public void Hash_And_Name_AreStable()
        {
            Assert.IsNotNull(NativeContract.Treasury.Hash);
            Assert.AreNotEqual(UInt160.Zero, NativeContract.Treasury.Hash);
            Assert.AreEqual(nameof(Treasury), NativeContract.Treasury.Name);
        }

        [TestMethod]
        public void IsNative_RecognizesTreasury()
        {
            Assert.IsTrue(NativeContract.IsNative(NativeContract.Treasury.Hash));
            Assert.AreSame(NativeContract.Treasury, NativeContract.GetContract(NativeContract.Treasury.Hash));
        }
    }
}
