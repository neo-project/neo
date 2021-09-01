using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Signers
    {
        [TestMethod]
        public void Serialize_Deserialize_Global()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.Global,
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000080";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<Signer>();

            Assert.AreEqual(attr.ToArray().Length, attr.Size);
            Assert.AreEqual(attr.Scopes, copy.Scopes);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CalledByEntry()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CalledByEntry,
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000001";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<Signer>();

            Assert.AreEqual(attr.ToArray().Length, attr.Size);
            Assert.AreEqual(attr.Scopes, copy.Scopes);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CustomContracts()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = new[] { UInt160.Zero },
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000010010000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<Signer>();

            Assert.AreEqual(attr.ToArray().Length, attr.Size);
            Assert.AreEqual(attr.Scopes, copy.Scopes);
            CollectionAssert.AreEqual(attr.AllowedContracts, copy.AllowedContracts);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CustomGroups()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomGroups,
                AllowedGroups = new[] { ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1) },
                Account = UInt160.Zero
            };

            var hex = "0000000000000000000000000000000000000000200103b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<Signer>();

            Assert.AreEqual(attr.ToArray().Length, attr.Size);
            Assert.AreEqual(attr.Scopes, copy.Scopes);
            CollectionAssert.AreEqual(attr.AllowedGroups, copy.AllowedGroups);
            Assert.AreEqual(attr.Account, copy.Account);
        }


        [TestMethod]
        public void Serialize_Deserialize_CustomCallingContracts()
        {
            var contract = UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5");
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomCallingContracts,
                AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                {
                    [contract] = new UInt160[] { UInt160.Zero }
                },
                Account = UInt160.Zero
            };

            var hex = "00000000000000000000000000000000000000000201f563ea40bc283d4d0e05c48ea305b3f2a07340ef010000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<Signer>();

            Assert.AreEqual(attr.ToArray().Length, attr.Size);
            Assert.AreEqual(attr.Scopes, copy.Scopes);
            Assert.AreEqual(attr.Account, copy.Account);
            Assert.AreEqual(1, copy.AllowedCallingContracts.Count);
            Assert.AreEqual(1, copy.AllowedCallingContracts[contract].Length);
            Assert.AreEqual(attr.AllowedCallingContracts[contract][0], copy.AllowedCallingContracts[contract][0]);
        }


        [TestMethod]
        public void Serialize_Deserialize_CustomCallingGroup()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var contract = UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5");
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomCallingGroups,
                AllowedCallingGroup = new Dictionary<UInt160, UInt160[]>()
                {
                    [contract] = new[] { Contract.CreateSignatureContract(point).ScriptHash }
                },
                Account = UInt160.Zero
            };

            var hex = "00000000000000000000000000000000000000000401f563ea40bc283d4d0e05c48ea305b3f2a07340ef0196949ed482e7c60aaeec691550f1b3d599146194";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<Signer>();

            Assert.AreEqual(attr.ToArray().Length, attr.Size);
            Assert.AreEqual(attr.Scopes, copy.Scopes);
            Assert.AreEqual(attr.Account, copy.Account);
            Assert.AreEqual(1, copy.AllowedCallingGroup.Count);
            Assert.AreEqual(1, copy.AllowedCallingGroup[contract].Length);
            Assert.AreEqual(attr.AllowedCallingGroup[contract][0], copy.AllowedCallingGroup[contract][0]);
        }

        [TestMethod]
        public void Json_Global()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.Global,
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"Global\"}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CalledByEntry()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CalledByEntry,
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CalledByEntry\"}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CustomContracts()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomContracts,
                AllowedContracts = new[] { UInt160.Zero },
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CustomContracts\",\"allowedcontracts\":[\"0x0000000000000000000000000000000000000000\"]}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CustomGroups()
        {
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomGroups,
                AllowedGroups = new[] { ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1) },
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CustomGroups\",\"allowedgroups\":[\"03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c\"]}";
            attr.ToJson().ToString().Should().Be(json);
        }



        [TestMethod]
        public void Json_CustomCallingContracts()
        {
            var contract = UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5");
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomCallingContracts,
                AllowedCallingContracts = new Dictionary<UInt160, UInt160[]>()
                {
                    [contract] = new UInt160[] { UInt160.Zero }
                },
                Account = UInt160.Zero
            };

            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CustomCallingContracts\",\"allowedcallingcontracts\":[{\"contract\":\"0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5\",\"trusts\":[\"0x0000000000000000000000000000000000000000\"]}]}";
            attr.ToJson().ToString().Should().Be(json);
        }

        [TestMethod]
        public void Json_CustomCallingGroup()
        {
            var point = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1);
            var contract = UInt160.Parse("0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5");
            var attr = new Signer()
            {
                Scopes = WitnessScope.CustomCallingGroups,
                AllowedCallingGroup = new Dictionary<UInt160, UInt160[]>()
                {
                    [contract] = new[] { Contract.CreateSignatureContract(point).ScriptHash }
                },
                Account = UInt160.Zero
            };


            var json = "{\"account\":\"0x0000000000000000000000000000000000000000\",\"scopes\":\"CustomCallingGroups\",\"allowedcallinggroups\":[{\"contract\":\"0xef4073a0f2b305a38ec4050e4d3d28bc40ea63f5\",\"trusts\":[\"0x94611499d5b3f1501569ecae0ac6e782d49e9496\"]}]}";
            attr.ToJson().ToString().Should().Be(json);
        }
    }
}
