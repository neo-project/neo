// Copyright (C) 2015-2024 The Neo Project.
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
using Neo.Json;
using Neo.Plugins.RpcServer.Model;
using Neo.UnitTests;
using Neo.Wallets;
using System;

namespace Neo.Plugins.RpcServer.Tests;

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

        JToken token = 1;
        Assert.AreEqual(1, ((ContractNameOrHashOrId)ParameterConverter.ConvertParameter(token, typeof(ContractNameOrHashOrId))).AsId());

        JToken token2 = "1";
        Assert.AreEqual(1, ((ContractNameOrHashOrId)ParameterConverter.ConvertParameter(token2, typeof(ContractNameOrHashOrId))).AsId());

        JToken token3 = "0x1234567890abcdef1234567890abcdef12345678";
        Assert.AreEqual(UInt160.Parse("0x1234567890abcdef1234567890abcdef12345678"), ((ContractNameOrHashOrId)ParameterConverter.ConvertParameter(token3, typeof(ContractNameOrHashOrId))).AsHash());

        JToken token4 = "0xabc";
        Assert.ThrowsException<RpcException>(() => ((ContractNameOrHashOrId)ParameterConverter.ConvertParameter(token4, typeof(ContractNameOrHashOrId))).AsHash());
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

        JToken token = 1;
        Assert.AreEqual(1u, ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token, typeof(BlockHashOrIndex))).AsIndex());

        JToken token2 = -1;
        Assert.ThrowsException<RpcException>(() => ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token2, typeof(BlockHashOrIndex))).AsIndex());

        JToken token3 = "1";
        Assert.AreEqual(1u, ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token3, typeof(BlockHashOrIndex))).AsIndex());

        JToken token4 = "-1";
        Assert.ThrowsException<RpcException>(() => ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token4, typeof(BlockHashOrIndex))).AsIndex());

        JToken token5 = "0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d";
        Assert.AreEqual(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"), ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token5, typeof(BlockHashOrIndex))).AsHash());

        JToken token6 = "761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d";
        Assert.AreEqual(UInt256.Parse("0x761a9bb72ca2a63984db0cc43f943a2a25e464f62d1a91114c2b6fbbfd24b51d"), ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token6, typeof(BlockHashOrIndex))).AsHash());

        JToken token7 = "0xabc";
        Assert.ThrowsException<RpcException>(() => ((BlockHashOrIndex)ParameterConverter.ConvertParameter(token7, typeof(BlockHashOrIndex))).AsHash());
    }

    [TestMethod]
    public void TestUInt160()
    {
        JToken token = "0x1234567890abcdef1234567890abcdef12345678";
        Assert.AreEqual(UInt160.Parse("0x1234567890abcdef1234567890abcdef12345678"), ParameterConverter.ConvertUInt160(token, TestProtocolSettings.Default.AddressVersion));

        JToken token2 = "0xabc";
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertUInt160(token2, TestProtocolSettings.Default.AddressVersion));

        JToken token3 = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
        Assert.AreEqual("NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf".ToScriptHash(TestProtocolSettings.Default.AddressVersion), ParameterConverter.ConvertUInt160(token3, TestProtocolSettings.Default.AddressVersion));
    }

    [TestMethod]
    public void TestUInt256()
    {
        JToken token = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        Assert.AreEqual(UInt256.Parse("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"), ParameterConverter.ConvertParameter(token, typeof(UInt256)));

        JToken token2 = "0xabc";
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(UInt256)));
    }

    [TestMethod]
    public void TestInteger()
    {
        JToken token = 1;
        Assert.AreEqual(1, ParameterConverter.ConvertParameter(token, typeof(int)));
        Assert.AreEqual((long)1, ParameterConverter.ConvertParameter(token, typeof(long)));
        Assert.AreEqual((uint)1, ParameterConverter.ConvertParameter(token, typeof(uint)));
        Assert.AreEqual((ulong)1, ParameterConverter.ConvertParameter(token, typeof(ulong)));
        Assert.AreEqual((short)1, ParameterConverter.ConvertParameter(token, typeof(short)));
        Assert.AreEqual((ushort)1, ParameterConverter.ConvertParameter(token, typeof(ushort)));
        Assert.AreEqual((byte)1, ParameterConverter.ConvertParameter(token, typeof(byte)));
        Assert.AreEqual((sbyte)1, ParameterConverter.ConvertParameter(token, typeof(sbyte)));

        JToken token2 = 1.1;

        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(long)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(uint)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(ulong)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(short)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(ushort)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(byte)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token2, typeof(sbyte)));

        JToken token3 = "1";

        Assert.AreEqual((int)1, ParameterConverter.ConvertParameter(token3, typeof(int)));
        Assert.AreEqual((long)1, ParameterConverter.ConvertParameter(token3, typeof(long)));
        Assert.AreEqual((uint)1, ParameterConverter.ConvertParameter(token3, typeof(uint)));
        Assert.AreEqual((ulong)1, ParameterConverter.ConvertParameter(token3, typeof(ulong)));
        Assert.AreEqual((short)1, ParameterConverter.ConvertParameter(token3, typeof(short)));
        Assert.AreEqual((ushort)1, ParameterConverter.ConvertParameter(token3, typeof(ushort)));
        Assert.AreEqual((byte)1, ParameterConverter.ConvertParameter(token3, typeof(byte)));
        Assert.AreEqual((sbyte)1, ParameterConverter.ConvertParameter(token3, typeof(sbyte)));

        JToken token4 = "1.1";
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(long)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(uint)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(ulong)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(short)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(ushort)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(byte)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token4, typeof(sbyte)));

        JToken token5 = "abc";

        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(long)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(uint)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(ulong)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(short)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(ushort)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(byte)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token5, typeof(sbyte)));

        JToken token6 = -1;

        Assert.AreEqual(-1, ParameterConverter.ConvertParameter(token6, typeof(int)));
        Assert.AreEqual((long)-1, ParameterConverter.ConvertParameter(token6, typeof(long)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token6, typeof(uint)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token6, typeof(ulong)));
        Assert.AreEqual((short)-1, ParameterConverter.ConvertParameter(token6, typeof(short)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token6, typeof(ushort)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(token6, typeof(byte)));
        Assert.AreEqual((sbyte)-1, ParameterConverter.ConvertParameter(token6, typeof(sbyte)));
    }

    [TestMethod]
    public void TestBoolean()
    {
        JToken token = true;
        Assert.AreEqual(true, ParameterConverter.ConvertParameter(token, typeof(bool)));
        JToken token2 = false;
        Assert.AreEqual(false, ParameterConverter.ConvertParameter(token2, typeof(bool)));
        JToken token6 = 1;
        Assert.AreEqual(true, ParameterConverter.ConvertParameter(token6, typeof(bool)));
        JToken token7 = 0;
        Assert.AreEqual(false, ParameterConverter.ConvertParameter(token7, typeof(bool)));
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
        JToken maxToken = int.MaxValue;
        Assert.AreEqual(int.MaxValue, ParameterConverter.ConvertParameter(maxToken, typeof(int)));

        // Test min value
        JToken minToken = int.MinValue;
        Assert.AreEqual(int.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(int)));

        // Test overflow
        JToken overflowToken = (long)int.MaxValue + 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(int)));

        // Test underflow
        JToken underflowToken = (long)int.MinValue - 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(int)));
    }

    private void TestByteConversions()
    {
        // Test max value
        JToken maxToken = byte.MaxValue;
        Assert.AreEqual(byte.MaxValue, ParameterConverter.ConvertParameter(maxToken, typeof(byte)));

        // Test min value
        JToken minToken = byte.MinValue;
        Assert.AreEqual(byte.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(byte)));

        // Test overflow
        JToken overflowToken = (int)byte.MaxValue + 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(byte)));

        // Test underflow
        JToken underflowToken = -1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(byte)));
    }

    private void TestSByteConversions()
    {
        // Test max value
        JToken maxToken = sbyte.MaxValue;
        Assert.AreEqual(sbyte.MaxValue, ParameterConverter.ConvertParameter(maxToken, typeof(sbyte)));

        // Test min value
        JToken minToken = sbyte.MinValue;
        Assert.AreEqual(sbyte.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(sbyte)));

        // Test overflow
        JToken overflowToken = (int)sbyte.MaxValue + 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(sbyte)));

        // Test underflow
        JToken underflowToken = (int)sbyte.MinValue - 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(sbyte)));
    }

    private void TestShortConversions()
    {
        // Test max value
        JToken maxToken = short.MaxValue;
        Assert.AreEqual(short.MaxValue, ParameterConverter.ConvertParameter(maxToken, typeof(short)));

        // Test min value
        JToken minToken = short.MinValue;
        Assert.AreEqual(short.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(short)));

        // Test overflow
        JToken overflowToken = (int)short.MaxValue + 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(short)));

        // Test underflow
        JToken underflowToken = (int)short.MinValue - 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(short)));
    }

    private void TestUShortConversions()
    {
        // Test max value
        JToken maxToken = ushort.MaxValue;
        Assert.AreEqual(ushort.MaxValue, ParameterConverter.ConvertParameter(maxToken, typeof(ushort)));

        // Test min value
        JToken minToken = ushort.MinValue;
        Assert.AreEqual(ushort.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(ushort)));

        // Test overflow
        JToken overflowToken = (int)ushort.MaxValue + 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(ushort)));

        // Test underflow
        JToken underflowToken = -1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(ushort)));
    }

    private void TestUIntConversions()
    {
        // Test max value
        JToken maxToken = uint.MaxValue;
        Assert.AreEqual(uint.MaxValue, ParameterConverter.ConvertParameter(maxToken, typeof(uint)));

        // Test min value
        JToken minToken = uint.MinValue;
        Assert.AreEqual(uint.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(uint)));

        // Test overflow
        JToken overflowToken = (ulong)uint.MaxValue + 1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(uint)));

        // Test underflow
        JToken underflowToken = -1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(uint)));
    }

    private void TestLongConversions()
    {
        // Test max value
        JToken maxToken = JNumber.MAX_SAFE_INTEGER;
        Assert.AreEqual(JNumber.MAX_SAFE_INTEGER, ParameterConverter.ConvertParameter(maxToken, typeof(long)));

        // Test min value
        JToken minToken = JNumber.MIN_SAFE_INTEGER;
        Assert.AreEqual(JNumber.MIN_SAFE_INTEGER, ParameterConverter.ConvertParameter(minToken, typeof(long)));

        // Test overflow
        JToken overflowToken = $"{JNumber.MAX_SAFE_INTEGER}0"; // This will be parsed as a string, causing overflow
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(long)));

        // Test underflow
        JToken underflowToken = $"-{JNumber.MIN_SAFE_INTEGER}0"; // This will be parsed as a string, causing underflow
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(long)));
    }

    private void TestULongConversions()
    {
        // Test max value
        JToken maxToken = JNumber.MAX_SAFE_INTEGER;
        Assert.AreEqual((ulong)JNumber.MAX_SAFE_INTEGER, ParameterConverter.ConvertParameter(maxToken, typeof(ulong)));

        // Test min value
        JToken minToken = ulong.MinValue;
        Assert.AreEqual(ulong.MinValue, ParameterConverter.ConvertParameter(minToken, typeof(ulong)));

        // Test overflow
        JToken overflowToken = $"{JNumber.MAX_SAFE_INTEGER}0"; // This will be parsed as a string, causing overflow
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(overflowToken, typeof(ulong)));

        // Test underflow
        JToken underflowToken = -1;
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(underflowToken, typeof(ulong)));
    }

    [TestMethod]
    public void TestAdditionalEdgeCases()
    {
        // Test conversion of fractional values slightly less than integers
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(0.9999999999999, typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(-0.0000000000001, typeof(int)));

        // Test conversion of very large double values to integer types
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(double.MaxValue, typeof(long)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(double.MinValue, typeof(long)));

        // Test conversion of NaN and Infinity
        Assert.ThrowsException<FormatException>(() => ParameterConverter.ConvertParameter(double.NaN, typeof(int)));
        Assert.ThrowsException<FormatException>(() => ParameterConverter.ConvertParameter(double.PositiveInfinity, typeof(long)));
        Assert.ThrowsException<FormatException>(() => ParameterConverter.ConvertParameter(double.NegativeInfinity, typeof(ulong)));

        // Test conversion of string representations of numbers
        Assert.AreEqual(int.MaxValue, ParameterConverter.ConvertParameter(int.MaxValue.ToString(), typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(long.MinValue.ToString(), typeof(long)));

        // Test conversion of hexadecimal string representations
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter("0xFF", typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter("0x100", typeof(byte)));

        // Test conversion of whitespace-padded strings
        Assert.AreEqual(42, ParameterConverter.ConvertParameter("  42  ", typeof(int)));
        Assert.AreEqual(42, ParameterConverter.ConvertParameter(" 42.0 ", typeof(int)));

        // Test conversion of empty or null values
        Assert.AreEqual(0, ParameterConverter.ConvertParameter("", typeof(int)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(JToken.Null, typeof(int)));

        // Test conversion to non-numeric types
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter(42, typeof(DateTime)));

        // Test conversion of values just outside the safe integer range for long and ulong
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter((double)long.MaxValue, typeof(long)));
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter((double)ulong.MaxValue, typeof(ulong)));

        // Test conversion of scientific notation
        Assert.AreEqual(1000000, ParameterConverter.ConvertParameter("1e6", typeof(int)));
        Assert.AreEqual(150, ParameterConverter.ConvertParameter("1.5e2", typeof(int)));

        // Test conversion of boolean values to numeric types
        Assert.AreEqual(1, ParameterConverter.ConvertParameter(true, typeof(int)));
        Assert.AreEqual(0, ParameterConverter.ConvertParameter(false, typeof(int)));

        // Test conversion of Unicode numeric characters
        Assert.ThrowsException<RpcException>(() => ParameterConverter.ConvertParameter("１２３４", typeof(int)));
    }
}
