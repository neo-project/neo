// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.SmartContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using System;
using System.Text;

namespace Neo.Plugins.RpcServer.Tests
{
    partial class UT_RpcServer
    {
        [TestMethod]
        public void TestInvokeFunction()
        {
            var result = _rpcServer.InvokeFunction(new JArray(NeoToken.NEO.Hash.ToString(), nameof(NeoToken.NEO.Symbol).ToLower()));
            result["state"].Should().Be("HALT");
            result["exception"].Should().BeNull();
            result["stack"][0]["type"].AsString().Should().Be(nameof(Neo.VM.Types.ByteString));
            Encoding.UTF8.GetString(Convert.FromBase64String(result["stack"][0]["value"].AsString())).Should().Be(NeoToken.NEO.Symbol);

            result = _rpcServer.InvokeFunction(new JArray(GasToken.GAS.Hash.ToString(), nameof(GasToken.GAS.Symbol).ToLower()));
            result["state"].Should().Be("HALT");
            result["exception"].Should().BeNull();
            result["stack"][0]["type"].AsString().Should().Be(nameof(Neo.VM.Types.ByteString));
            Encoding.UTF8.GetString(Convert.FromBase64String(result["stack"][0]["value"].AsString())).Should().Be(GasToken.GAS.Symbol);
        }
    }
}
