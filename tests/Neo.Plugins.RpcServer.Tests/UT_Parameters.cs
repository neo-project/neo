// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Parameters.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.RpcServer.Model;
using Neo.SmartContract;
using Neo.UnitTests;
using Neo.Wallets;
using System;
using System.Text.Json.Nodes;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public class UT_Parameters
    {
        [TestMethod]
        public void TestTryParse_ContractNameOrHashOrId()
        {
            Assert.IsTrue(ContractNameOrHashOrId.TryParse("1", out var contractNameOrHashOrId));
            Assert.IsTrue(contractNameOrHashOrId.IsId);
            Assert.IsTrue(ContractNameOrHashOrId.TryParse("0x1234567890abcdef1234567890abcdef12345678", out contractNameOrHashOrId));
            Assert.IsTrue(contractNameOrHashOrId.IsHash);
            Assert.IsTrue(ContractNameOrHashOrId.TryParse("test", out contractNameOrHashOrId));
            Assert.IsTrue(contractNameOrHashOrId.IsName);
            Assert.IsFalse(ContractNameOrHashOrId.TryParse("", out _));

            JsonNode token = 1;
            Assert.AreEqual(1, ((ContractNameOrHashOrId)token.AsParameter(typeof(ContractNameOrHashOrId))).AsId());

            JsonNode token2 = "1";
            Assert.AreEqual(1, ((ContractNameOrHashOrId)token2.AsParameter(typeof(ContractNameOrHashOrId))).AsId());

            JsonNode token3 = "0x1234567890abcdef1234567890abcdef12345678";
            Assert.AreEqual(UInt160.Parse("0x1234567890abcdef1234567890abcdef12345678"),
                ((ContractNameOrHashOrId)token3.AsParameter(typeof(ContractNameOrHashOrId))).AsHash());

            JsonNode token4 = "0xabc";
            Assert.ThrowsExactly<RpcException>(
                () => _ = ((ContractNameOrHashOrId)token4.AsParameter(typeof(ContractNameOrHashOrId))).AsHash());
        }

        [TestMethod]
        public void TestTryParse_BlockHashOrIndex()
        {
            Assert.IsTrue(BlockHashOrIndex.TryParse("1", out var blockHashOrIndex));
            Assert.IsTrue(blockHashOrIndex.IsIndex);
            Assert.AreEqual(1u, blockHashOrIndex.AsIndex());
            Assert.IsTrue(BlockHashOrIndex.TryParse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d", out blockHashOrIndex));
            Assert.AreEqual(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"), blockHashOrIndex.AsHash());
            Assert.IsFalse(BlockHashOrIndex.TryParse("", out _));

            JsonNode token = 1;
            Assert.AreEqual(1u, ((BlockHashOrIndex)token.AsParameter(typeof(BlockHashOrIndex))).AsIndex());

            JsonNode token2 = -1;
            Assert.ThrowsExactly<RpcException>(
                () => _ = ((BlockHashOrIndex)token2.AsParameter(typeof(BlockHashOrIndex))).AsIndex());

            JsonNode token3 = "1";
            Assert.AreEqual(1u, ((BlockHashOrIndex)token3.AsParameter(typeof(BlockHashOrIndex))).AsIndex());

            JsonNode token4 = "-1";
            Assert.ThrowsExactly<RpcException>(
                () => _ = ((BlockHashOrIndex)token4.AsParameter(typeof(BlockHashOrIndex))).AsIndex());

            JsonNode token5 = "0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d";
            Assert.AreEqual(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"),
                ((BlockHashOrIndex)token5.AsParameter(typeof(BlockHashOrIndex))).AsHash());

            JsonNode token6 = "761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d";
            Assert.AreEqual(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"),
                ((BlockHashOrIndex)token6.AsParameter(typeof(BlockHashOrIndex))).AsHash());

            JsonNode token7 = "0xabc";
            Assert.ThrowsExactly<RpcException>(
                () => _ = ((BlockHashOrIndex)ParameterConverter.AsParameter(token7, typeof(BlockHashOrIndex))).AsHash());
        }

        [TestMethod]
        public void TestUInt160()
        {
            JsonNode token = "0x1234567890abcdef1234567890abcdef12345678";
            Assert.AreEqual(UInt160.Parse("0x1234567890abcdef1234567890abcdef12345678"),
                (UInt160)token.AsParameter(typeof(UInt160)));

            var addressVersion = TestProtocolSettings.Default.AddressVersion;
            JsonNode token2 = "0xabc";
            Assert.ThrowsExactly<RpcException>(() => _ = token2.ToAddress(addressVersion));

            const string address = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            Assert.AreEqual(address.ToScriptHash(addressVersion), ((JsonNode)address).ToAddress(addressVersion).ScriptHash);
        }

        [TestMethod]
        public void TestUInt256()
        {
            JsonNode token = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
            Assert.AreEqual(UInt256.Parse("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"),
                token.AsParameter(typeof(UInt256)));

            JsonNode token2 = "0xabc";
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(UInt256)));
        }

        [TestMethod]
        public void TestInteger()
        {
            JsonNode token = 1;
            Assert.AreEqual(1, token.AsParameter(typeof(int)));
            Assert.AreEqual((long)1, token.AsParameter(typeof(long)));
            Assert.AreEqual((uint)1, token.AsParameter(typeof(uint)));
            Assert.AreEqual((ulong)1, token.AsParameter(typeof(ulong)));
            Assert.AreEqual((short)1, token.AsParameter(typeof(short)));
            Assert.AreEqual((ushort)1, token.AsParameter(typeof(ushort)));
            Assert.AreEqual((byte)1, token.AsParameter(typeof(byte)));
            Assert.AreEqual((sbyte)1, token.AsParameter(typeof(sbyte)));

            JsonNode token2 = 1.1;

            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(long)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(uint)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(ulong)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(short)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(ushort)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(byte)));
            Assert.ThrowsExactly<RpcException>(() => _ = token2.AsParameter(typeof(sbyte)));

            JsonNode token3 = "1";

            Assert.AreEqual((int)1, token3.AsParameter(typeof(int)));
            Assert.AreEqual((long)1, token3.AsParameter(typeof(long)));
            Assert.AreEqual((uint)1, token3.AsParameter(typeof(uint)));
            Assert.AreEqual((ulong)1, token3.AsParameter(typeof(ulong)));
            Assert.AreEqual((short)1, token3.AsParameter(typeof(short)));
            Assert.AreEqual((ushort)1, token3.AsParameter(typeof(ushort)));
            Assert.AreEqual((byte)1, token3.AsParameter(typeof(byte)));
            Assert.AreEqual((sbyte)1, token3.AsParameter(typeof(sbyte)));

            JsonNode token4 = "1.1";
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(long)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(uint)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(ulong)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(short)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(ushort)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(byte)));
            Assert.ThrowsExactly<RpcException>(() => _ = token4.AsParameter(typeof(sbyte)));

            JsonNode token5 = "abc";

            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(long)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(uint)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(ulong)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(short)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(ushort)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(byte)));
            Assert.ThrowsExactly<RpcException>(() => _ = token5.AsParameter(typeof(sbyte)));

            JsonNode token6 = -1;

            Assert.AreEqual(-1, token6.AsParameter(typeof(int)));
            Assert.AreEqual((long)-1, token6.AsParameter(typeof(long)));
            Assert.ThrowsExactly<RpcException>(() => _ = token6.AsParameter(typeof(uint)));
            Assert.ThrowsExactly<RpcException>(() => _ = token6.AsParameter(typeof(ulong)));
            Assert.AreEqual((short)-1, token6.AsParameter(typeof(short)));
            Assert.ThrowsExactly<RpcException>(() => _ = token6.AsParameter(typeof(ushort)));
            Assert.ThrowsExactly<RpcException>(() => _ = token6.AsParameter(typeof(byte)));
            Assert.AreEqual((sbyte)-1, token6.AsParameter(typeof(sbyte)));
        }

        [TestMethod]
        public void TestBoolean()
        {
            JsonNode token = true;
            Assert.IsTrue((bool?)token.AsParameter(typeof(bool)));
            JsonNode token2 = false;
            Assert.IsFalse((bool?)token2.AsParameter(typeof(bool)));
            JsonNode token6 = 1;
            Assert.IsTrue((bool?)token6.AsParameter(typeof(bool)));
            JsonNode token7 = 0;
            Assert.IsFalse((bool?)ParameterConverter.AsParameter(token7, typeof(bool)));
        }

        [TestMethod]
        public void TestNumericTypeConversions()
        {
            // Test integer conversions
            TestIntegerConversions();

            // Test byte conversions
            TestByteConversions();

            // Test sbyte conversions
            TestSByteConversions();

            // Test short conversions
            TestShortConversions();

            // Test ushort conversions
            TestUShortConversions();

            // Test uint conversions
            TestUIntConversions();

            // Test long conversions
            TestLongConversions();

            // Test ulong conversions
            TestULongConversions();
        }

        private void TestIntegerConversions()
        {
            // Test max value
            JsonNode maxToken = int.MaxValue;
            Assert.AreEqual(int.MaxValue, ParameterConverter.AsParameter(maxToken, typeof(int)));

            // Test min value
            JsonNode minToken = int.MinValue;
            Assert.AreEqual(int.MinValue, ParameterConverter.AsParameter(minToken, typeof(int)));

            // Test overflow
            JsonNode overflowToken = (long)int.MaxValue + 1;
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(int)));

            // Test underflow
            JsonNode underflowToken = (long)int.MinValue - 1;
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(int)));
        }

        private void TestByteConversions()
        {
            // Test max value
            JsonNode maxToken = byte.MaxValue;
            Assert.AreEqual(byte.MaxValue, maxToken.AsParameter(typeof(byte)));

            // Test min value
            JsonNode minToken = byte.MinValue;
            Assert.AreEqual(byte.MinValue, minToken.AsParameter(typeof(byte)));

            // Test overflow
            JsonNode overflowToken = (int)byte.MaxValue + 1;
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(byte)));

            // Test underflow
            JsonNode underflowToken = -1;
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(byte)));
        }

        private void TestSByteConversions()
        {
            // Test max value
            JsonNode maxToken = sbyte.MaxValue;
            Assert.AreEqual(sbyte.MaxValue, maxToken.AsParameter(typeof(sbyte)));

            // Test min value
            JsonNode minToken = sbyte.MinValue;
            Assert.AreEqual(sbyte.MinValue, minToken.AsParameter(typeof(sbyte)));

            // Test overflow
            JsonNode overflowToken = (int)sbyte.MaxValue + 1;
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(sbyte)));

            // Test underflow
            JsonNode underflowToken = (int)sbyte.MinValue - 1;
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(sbyte)));
        }

        private void TestShortConversions()
        {
            // Test max value
            JsonNode maxToken = short.MaxValue;
            Assert.AreEqual(short.MaxValue, maxToken.AsParameter(typeof(short)));

            // Test min value
            JsonNode minToken = short.MinValue;
            Assert.AreEqual(short.MinValue, minToken.AsParameter(typeof(short)));

            // Test overflow
            JsonNode overflowToken = (int)short.MaxValue + 1;
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(short)));

            // Test underflow
            JsonNode underflowToken = (int)short.MinValue - 1;
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(short)));
        }

        private void TestUShortConversions()
        {
            // Test max value
            JsonNode maxToken = ushort.MaxValue;
            Assert.AreEqual(ushort.MaxValue, maxToken.AsParameter(typeof(ushort)));

            // Test min value
            JsonNode minToken = ushort.MinValue;
            Assert.AreEqual(ushort.MinValue, minToken.AsParameter(typeof(ushort)));

            // Test overflow
            JsonNode overflowToken = (int)ushort.MaxValue + 1;
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(ushort)));

            // Test underflow
            JsonNode underflowToken = -1;
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(ushort)));
        }

        private void TestUIntConversions()
        {
            // Test max value
            JsonNode maxToken = uint.MaxValue;
            Assert.AreEqual(uint.MaxValue, maxToken.AsParameter(typeof(uint)));

            // Test min value
            JsonNode minToken = uint.MinValue;
            Assert.AreEqual(uint.MinValue, minToken.AsParameter(typeof(uint)));

            // Test overflow
            JsonNode overflowToken = (ulong)uint.MaxValue + 1;
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(uint)));

            // Test underflow
            JsonNode underflowToken = -1;
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(uint)));
        }

        private void TestLongConversions()
        {
            // Test max value
            JsonNode maxToken = JsonConstants.MAX_SAFE_INTEGER;
            Assert.AreEqual(JsonConstants.MAX_SAFE_INTEGER, maxToken.AsParameter(typeof(long)));

            // Test min value
            JsonNode minToken = JsonConstants.MIN_SAFE_INTEGER;
            Assert.AreEqual(JsonConstants.MIN_SAFE_INTEGER, minToken.AsParameter(typeof(long)));

            // Test overflow
            JsonNode overflowToken = $"{JsonConstants.MAX_SAFE_INTEGER}0"; // This will be parsed as a string, causing overflow
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(long)));

            // Test underflow
            JsonNode underflowToken = $"-{JsonConstants.MIN_SAFE_INTEGER}0"; // This will be parsed as a string, causing underflow
            Assert.ThrowsExactly<RpcException>(() => _ = underflowToken.AsParameter(typeof(long)));
        }

        private void TestULongConversions()
        {
            // Test max value
            JsonNode maxToken = JsonConstants.MAX_SAFE_INTEGER;
            Assert.AreEqual((ulong)JsonConstants.MAX_SAFE_INTEGER, maxToken.AsParameter(typeof(ulong)));

            // Test min value
            JsonNode minToken = ulong.MinValue;
            Assert.AreEqual(ulong.MinValue, minToken.AsParameter(typeof(ulong)));

            // Test overflow
            JsonNode overflowToken = $"{JsonConstants.MAX_SAFE_INTEGER}0"; // This will be parsed as a string, causing overflow
            Assert.ThrowsExactly<RpcException>(() => _ = overflowToken.AsParameter(typeof(ulong)));

            // Test underflow
            JsonNode underflowToken = -1;
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(underflowToken, typeof(ulong)));
        }

        [TestMethod]
        public void TestAdditionalEdgeCases()
        {
            // Test conversion of fractional values slightly less than integers
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(0.9999999999999, typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(-0.0000000000001, typeof(int)));

            // Test conversion of very large double values to integer types
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(double.MaxValue, typeof(long)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(double.MinValue, typeof(long)));

            // Test conversion of NaN and Infinity
            Assert.ThrowsExactly<FormatException>(() => _ = ParameterConverter.AsParameter(double.NaN, typeof(int)));
            Assert.ThrowsExactly<FormatException>(() => _ = ParameterConverter.AsParameter(double.PositiveInfinity, typeof(long)));
            Assert.ThrowsExactly<FormatException>(() => _ = ParameterConverter.AsParameter(double.NegativeInfinity, typeof(ulong)));

            // Test conversion of string representations of numbers
            Assert.AreEqual(int.MaxValue, ParameterConverter.AsParameter(int.MaxValue.ToString(), typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(long.MinValue.ToString(), typeof(long)));

            // Test conversion of hexadecimal string representations
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter("0xFF", typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter("0x100", typeof(byte)));

            // Test conversion of whitespace-padded strings
            Assert.AreEqual(42, ParameterConverter.AsParameter("  42  ", typeof(int)));
            Assert.AreEqual(42, ParameterConverter.AsParameter(" 42.0 ", typeof(int)));

            // Test conversion of empty or null values
            Assert.AreEqual(0, ParameterConverter.AsParameter("", typeof(int)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(null, typeof(int)));

            // Test conversion to non-numeric types
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter(42, typeof(DateTime)));

            // Test conversion of values just outside the safe integer range for long and ulong
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter((double)long.MaxValue, typeof(long)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter((double)ulong.MaxValue, typeof(ulong)));

            // Test conversion of scientific notation
            Assert.AreEqual(1000000, ParameterConverter.AsParameter("1e6", typeof(int)));
            Assert.AreEqual(150, ParameterConverter.AsParameter("1.5e2", typeof(int)));

            // Test conversion of boolean values to numeric types
            Assert.AreEqual(1, ParameterConverter.AsParameter(true, typeof(int)));
            Assert.AreEqual(0, ParameterConverter.AsParameter(false, typeof(int)));

            // Test conversion of Unicode numeric characters
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter("１２３４", typeof(int)));
        }

        [TestMethod]
        public void TestToSignersAndWitnesses()
        {
            const string address = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            var addressVersion = TestProtocolSettings.Default.AddressVersion;
            var account = address.AddressToScriptHash(addressVersion);
            var signers = new JsonArray(new JsonObject
            {
                ["account"] = address,
                ["scopes"] = WitnessScope.CalledByEntry.ToString()
            });

            var result = signers.ToSignersAndWitnesses(addressVersion);
            Assert.HasCount(1, result.Signers);
            Assert.IsEmpty(result.Witnesses);
            Assert.AreEqual(account, result.Signers[0].Account);
            Assert.AreEqual(WitnessScope.CalledByEntry, result.Signers[0].Scopes);

            var signersAndWitnesses = new JsonArray(new JsonObject
            {
                ["account"] = address,
                ["scopes"] = WitnessScope.CalledByEntry.ToString(),
                ["invocation"] = "SGVsbG8K",
                ["verification"] = "V29ybGQK"
            });
            result = signersAndWitnesses.ToSignersAndWitnesses(addressVersion);
            Assert.HasCount(1, result.Signers);
            Assert.HasCount(1, result.Witnesses);
            Assert.AreEqual(account, result.Signers[0].Account);
            Assert.AreEqual(WitnessScope.CalledByEntry, result.Signers[0].Scopes);
            Assert.AreEqual("SGVsbG8K", Convert.ToBase64String(result.Witnesses[0].InvocationScript.Span));
            Assert.AreEqual("V29ybGQK", Convert.ToBase64String(result.Witnesses[0].VerificationScript.Span));
        }

        [TestMethod]
        public void TestAddressToScriptHash()
        {
            const string address = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            var addressVersion = TestProtocolSettings.Default.AddressVersion;
            var account = address.AddressToScriptHash(addressVersion);
            Assert.AreEqual(account, address.AddressToScriptHash(addressVersion));

            var hex = new UInt160().ToString();
            Assert.AreEqual(new UInt160(), hex.AddressToScriptHash(addressVersion));

            var base58 = account.ToAddress(addressVersion);
            Assert.AreEqual(account, base58.AddressToScriptHash(addressVersion));
        }

        [TestMethod]
        public void TestGuid()
        {
            var guid = Guid.NewGuid();
            Assert.AreEqual(guid, ParameterConverter.AsParameter(guid.ToString(), typeof(Guid)));
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter("abc", typeof(Guid)));
        }

        [TestMethod]
        public void TestBytes()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var parameter = ParameterConverter.AsParameter(Convert.ToBase64String(bytes), typeof(byte[]));
            Assert.AreEqual(bytes.ToHexString(), ((byte[])parameter).ToHexString());
            Assert.ThrowsExactly<RpcException>(() => _ = ParameterConverter.AsParameter("😊", typeof(byte[])));
        }

        [TestMethod]
        public void TestContractParameters()
        {
            var parameters = new JsonArray(new JsonObject
            {
                ["value"] = "test",
                ["type"] = "String"
            });

            var converted = (ContractParameter[])parameters.AsParameter(typeof(ContractParameter[]));
            Assert.AreEqual("test", converted[0].ToString());
            Assert.AreEqual(ContractParameterType.String, converted[0].Type);

            // Invalid Parameter
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonArray([null]).AsParameter(typeof(ContractParameter[])));
        }

        [TestMethod]
        public void TestToSigner()
        {
            const string address = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            var version = TestProtocolSettings.Default.AddressVersion;
            var account = address.AddressToScriptHash(version);
            var signer = new JsonObject
            {
                ["account"] = address,
                ["scopes"] = WitnessScope.CalledByEntry.ToString()
            };

            var got = signer.ToSigner(version);
            Assert.AreEqual(account, got.Account);
            Assert.AreEqual(WitnessScope.CalledByEntry, got.Scopes);

            // Invalid Parameter
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject().ToSigner(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject { ["account"] = address }.ToSigner(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject { ["scopes"] = "InvalidScopeValue" }.ToSigner(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject { ["allowedcontracts"] = "InvalidContractHash" }.ToSigner(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject { ["allowedgroups"] = "InvalidECPoint" }.ToSigner(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject { ["rules"] = "InvalidRule" }.ToSigner(version));
        }

        [TestMethod]
        public void TestToSigners()
        {
            var address = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            var version = TestProtocolSettings.Default.AddressVersion;
            var scopes = WitnessScope.CalledByEntry;
            var account = address.AddressToScriptHash(version);
            var signers = new JsonArray(new JsonObject { ["account"] = address, ["scopes"] = scopes.ToString() });
            var got = signers.ToSigners(version);
            Assert.HasCount(1, got);
            Assert.AreEqual(account, got[0].Account);
            Assert.AreEqual(scopes, got[0].Scopes);
        }

        [TestMethod]
        public void TestToAddresses()
        {
            var address = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            var version = TestProtocolSettings.Default.AddressVersion;
            var account = address.AddressToScriptHash(version);
            var got = new JsonArray(address).ToAddresses(version);
            Assert.HasCount(1, got);
            Assert.AreEqual(account, got[0].ScriptHash);

            // Invalid Parameter
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonObject().ToAddresses(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonArray([null]).ToAddresses(version));
            Assert.ThrowsExactly<RpcException>(() => _ = new JsonArray(["InvalidAddress"]).ToAddresses(version));
        }
    }
}
