using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using Neo.Wallets;
using System;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_Wallets_Helper
    {
        [TestMethod]
        public void TestToScriptHash()
        {
            byte[] array = { 0x01 };
            UInt160 scriptHash = new UInt160(Crypto.Hash160(array));
            "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf".ToScriptHash().Should().Be(scriptHash);

            Action action = () => "3vQB7B6MrGQZaxCuFg4oh".ToScriptHash();
            action.Should().Throw<FormatException>();
        }
    }
}
