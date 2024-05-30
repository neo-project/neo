// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.Blockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.Plugins.RpcServer.Tests;

public partial class UT_RpcServer
{
    [TestMethod]
    public void TestGetBlockHeaderCount()
    {
        _mockSystem.Setup(m => m.HeaderCache.Last).Returns(new Header { Index = 123 });

        var result = _rpcServer.GetBlockHeaderCount(new JArray());
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(JNumber));
        Assert.AreEqual(124, result.AsNumber());
    }
}
