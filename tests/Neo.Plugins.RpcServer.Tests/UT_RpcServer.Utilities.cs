// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using System;
using System.IO;
using System.Linq;

namespace Neo.Plugins.RpcServer.Tests
{
    public partial class UT_RpcServer
    {
        [TestMethod]
        public void TestListPlugins()
        {
            JArray resp = (JArray)_rpcServer.ListPlugins([]);
            Assert.AreEqual(resp.Count, 0);
            Plugins.Plugin.Plugins.Add(new RpcServer());
            resp = (JArray)_rpcServer.ListPlugins([]);
            Assert.AreEqual(resp.Count, 2);
            foreach (JObject p in resp)
                Assert.AreEqual(p["name"], nameof(Server));
        }

        [TestMethod]
        public void TestValidateAddress()
        {
            string validAddr = "NM7Aky765FG8NhhwtxjXRx7jEL1cnw7PBP";
            JObject resp = (JObject)_rpcServer.ValidateAddress([validAddr]);
            Assert.AreEqual(resp["address"], validAddr);
            Assert.AreEqual(resp["isvalid"], true);
            string invalidAddr = "ANeo2toNeo3MigrationAddressxwPB2Hz";
            resp = (JObject)_rpcServer.ValidateAddress([invalidAddr]);
            Assert.AreEqual(resp["address"], invalidAddr);
            Assert.AreEqual(resp["isvalid"], false);
        }
    }
}
