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
            "AZk5bAanTtD6AvpeesmYgL8CLRYUt5JQsX".ToScriptHash().Should().Be(scriptHash);

            Action action = () => "3vQB7B6MrGQZaxCuFg4oh".ToScriptHash();
            action.Should().Throw<FormatException>();

            var address = scriptHash.ToAddress();
            Span<byte> data = stackalloc byte[21];
            // NEO version is 0x17
            data[0] = 0x01;
            scriptHash.ToArray().CopyTo(data[1..]);
            address = Base58.Base58CheckEncode(data);
            action = () => address.ToScriptHash();
            action.Should().Throw<FormatException>();
        }
    }
}
