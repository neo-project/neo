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

namespace Neo.Plugins.RpcServer.Tests;

// ConvertParameter

[TestClass]
public class UT_Parameters
{
    private NeoSystem _neoSystem;

    [TestInitialize]
    public void TestSetup()
    {
        var _neoSystem = new NeoSystem(TestProtocolSettings.Default, new TestBlockchain.StoreProvider());
    }

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

}
