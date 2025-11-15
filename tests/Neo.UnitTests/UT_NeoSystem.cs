// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoSystem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P;

namespace Neo.UnitTests;

[TestClass]
public class UT_NeoSystem
{
    private NeoSystem _system = null!;

    [TestInitialize]
    public void Setup()
    {
        _system = TestBlockchain.GetSystem();
    }

    [TestMethod]
    public void TestGetBlockchain() => Assert.IsNotNull(_system.Blockchain);

    [TestMethod]
    public void TestGetLocalNode() => Assert.IsNotNull(_system.LocalNode);

    [TestMethod]
    public void TestGetTaskManager() => Assert.IsNotNull(_system.TaskManager);

    [TestMethod]
    public void TestAddAndGetService()
    {
        var service = new object();
        _system.AddService(service);

        var result = _system.GetService<object>();
        Assert.AreEqual(service, result);
    }

    [TestMethod]
    public void TestGetServiceWithFilter()
    {
        _system.AddService("match");
        _system.AddService("skip");

        var result = _system.GetService<string>(s => s == "match");
        Assert.AreEqual("match", result);
    }

    [TestMethod]
    public void TestResumeNodeStartup()
    {
        _system.SuspendNodeStartup();
        _system.SuspendNodeStartup();
        Assert.IsFalse(_system.ResumeNodeStartup());
        Assert.IsTrue(_system.ResumeNodeStartup()); // now it should resume
    }

    [TestMethod]
    public void TestStartNodeWhenNoSuspended()
    {
        var config = new ChannelsConfig();
        _system.StartNode(config);
    }

    [TestMethod]
    public void TestStartNodeWhenSuspended()
    {
        _system.SuspendNodeStartup();
        _system.SuspendNodeStartup();
        var config = new ChannelsConfig();
        _system.StartNode(config);
        Assert.IsFalse(_system.ResumeNodeStartup());
        Assert.IsTrue(_system.ResumeNodeStartup());
    }

    [TestMethod]
    public void TestEnsureStoppedStopsActor()
    {
        var sys = TestBlockchain.GetSystem();
        sys.EnsureStopped(sys.LocalNode);
    }

    [TestMethod]
    public void TestContainsTransactionNotExist()
    {
        var txHash = new UInt256(new byte[32]);
        var result = _system.ContainsTransaction(txHash);
        Assert.AreEqual(ContainsTransactionType.NotExist, result);
    }
}
