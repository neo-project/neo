// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;


namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_CryptoLib
    {

        [TestMethod]
        public void Check_PubKeyToScriptHash()
        {
            CryptoLib.PubKeyToScriptHash("03eecd363220d6e4de793abfebae09779768468c59a3b6e0132a9ea3b22010f7d8".HexToBytes(), NamedCurve.secp256r1).Should().Be(UInt160.Parse("0x9fa601726236a1d7ac081b65ff725942ee4aecaa"));
        }

        [TestMethod]
        public void Check_ScriptHashToAddress_True()
        {
            CryptoLib.ScriptHashToAddr(UInt160.Parse("0x9fa601726236a1d7ac081b65ff725942ee4aecaa"), 53).ToString().Should().Be("NbVj8GhwToNv4WF2gVaoco6hbkMQ8hrHWP");
        }

        [TestMethod]
        public void Check_ScriptHashToAddress_Error()
        {
            CryptoLib.ScriptHashToAddr(UInt160.Parse("0x9fa601726236a1d7ac081b65ff725942ee4aecaa"), 52).ToString().Should().NotBe("NbVj8GhwToNv4WF2gVaoco6hbkMQ8hrHWP");
        }
    }
}
