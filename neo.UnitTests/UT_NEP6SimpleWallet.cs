using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NEP6SimpleWallet
    {
        NEP6SimpleWallet uut;

        [TestInitialize]
        public void TestSetup()
        {
            string sjson = @"{""name"":""wallet1"",""version"":""1.0"",""scrypt"":{""n"":16384,""r"":8,""p"":8},""accounts"":[{""address"":""AKkkumHbBipZ46UMZJoFynJMXzSRnBvKcs"",""label"":null,""isDefault"":false,""lock"":false,""key"":""6PYLmjBYJ4wQTCEfqvnznGJwZeW9pfUcV5m5oreHxqryUgqKpTRAFt9L8Y"",""contract"":{""script"":""2102b3622bf4017bdfe317c58aed5f4c753f206b7db896046fa7d774bbc4bf7f8dc2ac"",""parameters"":[{""name"":""parameter0"",""type"":""Signature""}],""deployed"":false},""extra"":null},{""address"":""AZ81H31DMWzbSnFDLFkzh9vHwaDLayV7fU"",""label"":null,""isDefault"":false,""lock"":false,""key"":""6PYLmjBYJ4wQTCEfqvnznGJwZeW9pfUcV5m5oreHxqryUgqKpTRAFt9L8Y"",""contract"":{""script"":""532102103a7f7dd016558597f7960d27c516a4394fd968b9e65155eb4b013e4040406e2102a7bc55fe8684e0119768d104ba30795bdcc86619e864add26156723ed185cd622102b3622bf4017bdfe317c58aed5f4c753f206b7db896046fa7d774bbc4bf7f8dc22103d90c07df63e690ce77912e10ab51acc944b66860237b608c4f8f8309e71ee69954ae"",""parameters"":[{""name"":""parameter0"",""type"":""Signature""},{""name"":""parameter1"",""type"":""Signature""},{""name"":""parameter2"",""type"":""Signature""}],""deployed"":false},""extra"":null}],""extra"":null}";
            uut = new NEP6SimpleWallet(sjson);
            string password = "one";
            uut.Unlock(password);
        }

        [TestMethod]
        public void NEP6Accounts()
        {
            byte[] scripthash1 = new byte[]{0x2b,0xaa,0x76,0xad,0x53,0x4b,0x88,0x6c,0xb8,0x7c,0x6b,0x37,0x20,0xa3,0x49,0x43,0xd9,0x00,0x0f,0xa9};
            uut.GetAccount(new UInt160(scripthash1)).Address.Should().Be("AKkkumHbBipZ46UMZJoFynJMXzSRnBvKcs");
            byte[] scripthash2 = new byte[]{0xbe,0x48,0xd3,0xa3,0xf5,0xd1,0x00,0x13,0xab,0x9f,0xfe,0xe4,0x89,0x70,0x60,0x78,0x71,0x4f,0x1e,0xa2};
            uut.GetAccount(new UInt160(scripthash2)).Address.Should().Be("AZ81H31DMWzbSnFDLFkzh9vHwaDLayV7fU");
        }
    }
}
