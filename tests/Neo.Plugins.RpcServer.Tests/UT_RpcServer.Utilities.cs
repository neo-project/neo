// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcServer.Utilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;

namespace Neo.Plugins.RpcServer.Tests
{
    public partial class UT_RpcServer
    {
        [TestMethod]
        public void TestListPlugins()
        {
            var resp = (JArray)_rpcServer.ListPlugins();
            Assert.IsEmpty(resp);
            Plugin.Plugins.Add(new RpcServerPlugin());

            resp = (JArray)_rpcServer.ListPlugins();
            Assert.HasCount(2, resp);
            foreach (var p in resp)
                Assert.AreEqual(nameof(RpcServer), p["name"]);
        }

        [TestMethod]
        public void TestValidateAddress()
        {
            var validAddr = new JString("NM7Aky765FG8NhhwtxjXRx7jEL1cnw7PBP");
            var resp = (JObject)_rpcServer.ValidateAddress(validAddr.AsString());
            Assert.AreEqual(resp["address"], validAddr);
            Assert.AreEqual(true, resp["isvalid"]);

            var invalidAddr = "ANeo2toNeo3MigrationAddressxwPB2Hz";
            resp = (JObject)_rpcServer.ValidateAddress(invalidAddr);
            Assert.AreEqual(resp["address"], invalidAddr);
            Assert.AreEqual(false, resp["isvalid"]);
        }

        [TestMethod]
        public void TestValidateAddress_EmptyString()
        {
            var emptyAddr = "";
            var resp = (JObject)_rpcServer.ValidateAddress(emptyAddr);
            Assert.AreEqual(resp["address"], emptyAddr);
            Assert.AreEqual(false, resp["isvalid"]);
        }

        [TestMethod]
        public void TestValidateAddress_InvalidChecksum()
        {
            // Valid address: NM7Aky765FG8NhhwtxjXRx7jEL1cnw7PBP
            // Change last char to invalidate checksum
            var invalidChecksumAddr = "NM7Aky765FG8NhhwtxjXRx7jEL1cnw7PBO";
            var resp = (JObject)_rpcServer.ValidateAddress(invalidChecksumAddr);
            Assert.AreEqual(resp["address"], invalidChecksumAddr);
            Assert.AreEqual(false, resp["isvalid"]);
        }

        [TestMethod]
        public void TestValidateAddress_WrongLength()
        {
            // Address too short
            var shortAddr = "NM7Aky765FG8NhhwtxjXRx7jEL1cnw7P";
            var resp = (JObject)_rpcServer.ValidateAddress(shortAddr);
            Assert.AreEqual(resp["address"], shortAddr);
            Assert.AreEqual(false, resp["isvalid"]);

            // Address too long
            var longAddr = "NM7Aky765FG8NhhwtxjXRx7jEL1cnw7PBPPP";
            resp = (JObject)_rpcServer.ValidateAddress(longAddr);
            Assert.AreEqual(resp["address"], longAddr);
            Assert.AreEqual(false, resp["isvalid"]);
        }
    }
}
