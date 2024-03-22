// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NativeContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NativeContract
    {
        [TestMethod]
        public void TestGetContract()
        {
            Assert.IsTrue(NativeContract.NEO == NativeContract.GetContract(NativeContract.NEO.Hash));
        }

        [TestMethod]
        public void TestIsInitializeBlock()
        {
            string json = UT_ProtocolSettings.CreateHKSettings("\"HF_Cockatrice\": 20");

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);
            ProtocolSettings settings = ProtocolSettings.Load(file, false);
            File.Delete(file);

            Assert.IsTrue(NativeContract.CryptoLib.IsInitializeBlock(settings, 0, out var hf));
            Assert.IsNull(hf);

            Assert.IsFalse(NativeContract.CryptoLib.IsInitializeBlock(settings, 1, out hf));
            Assert.IsNull(hf);

            Assert.IsTrue(NativeContract.CryptoLib.IsInitializeBlock(settings, 20, out hf));
            Assert.AreEqual(Hardfork.HF_Cockatrice, hf);
        }
    }
}
