// Copyright (C) 2015-2025 The Neo Project.
//
// TestPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using System.Diagnostics.CodeAnalysis;

namespace Neo.UnitTests.Plugins;


internal class TestPluginSettings : IPluginSettings
{
    public static TestPluginSettings? Default { get; private set; }

    public UnhandledExceptionPolicy ExceptionPolicy => UnhandledExceptionPolicy.Ignore;

    [MemberNotNull(nameof(Default))]
    public static void Load(IConfigurationSection section)
    {
        Default = new TestPluginSettings();
    }
}
internal class TestNonPlugin
{
    public TestNonPlugin()
    {
        Blockchain.Committing += OnCommitting;
        Blockchain.Committed += OnCommitted;
    }

    private static void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
    {
        throw new NotImplementedException("Test exception from OnCommitting");
    }

    private static void OnCommitted(NeoSystem system, Block block)
    {
        throw new NotImplementedException("Test exception from OnCommitted");
    }
}


internal class TestPlugin : Plugin
{
    private readonly UnhandledExceptionPolicy _exceptionPolicy;
    protected internal override UnhandledExceptionPolicy ExceptionPolicy => _exceptionPolicy;

    public TestPlugin(UnhandledExceptionPolicy exceptionPolicy = UnhandledExceptionPolicy.StopPlugin)
    {
        Blockchain.Committing += OnCommitting;
        Blockchain.Committed += OnCommitted;
        _exceptionPolicy = exceptionPolicy;
    }

    protected override void Configure()
    {
        TestPluginSettings.Load(GetConfiguration());
    }

    public void LogMessage(string message)
    {
        Log(message);
    }

    public bool TestOnMessage(object message)
    {
        return OnMessage(message);
    }

    public IConfigurationSection TestGetConfiguration()
    {
        return GetConfiguration();
    }

    protected override bool OnMessage(object message) => true;

    private void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
    {
        throw new NotImplementedException();
    }

    private void OnCommitted(NeoSystem system, Block block)
    {
        throw new NotImplementedException();
    }
}
