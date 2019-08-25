using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
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
            UInt160 scriptHash = new UInt160(Crypto.Default.Hash160(array));
            "AZk5bAanTtD6AvpeesmYgL8CLRYUt5JQsX".ToScriptHash().Should().Be(scriptHash);

            Action action = () => "3vQB7B6MrGQZaxCuFg4oh".ToScriptHash();
            action.ShouldThrow<FormatException>();

            var address = scriptHash.ToAddress();
            byte[] data = new byte[21];
            // NEO version is 0x17
            data[0] = 0x01;
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            address = data.Base58CheckEncode();
            action = () => address.ToScriptHash();
            action.ShouldThrow<FormatException>();
        }
    }
}
