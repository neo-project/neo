// Copyright (C) 2015-2024 The Neo Project.
//
// UT_DBFT.Fields.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography.ECC;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests;

public partial class UT_DBFT
{
    public TestContext TestContext { get; set; }
    private NeoSystem _neoSystem;
    private readonly TestProbe _mockLocalNode;
    private readonly TestProbe _mockBlockchain;
    private readonly Settings _testSettings;
    private readonly TestActorRef<ConsensusService> _consensusService;
    private TestMemoryStoreProvider _memoryStoreProvider;
    private readonly MemoryStore _memoryStore;
    private readonly Mock<IActorRef> _mockTaskManager;
    private readonly Mock<IActorRef> _txRouter;
    private TestProbe _testProbe;
    private readonly ECPoint[] _testValidators;
    private UInt160 _testNextConsensus;
    private int _primaryIndex = 0;
    private static int s_currentIndex = 0;

    private TestWalletProvider[] _walletProviders;
    private readonly NEP6Wallet[] _wallets;
    private readonly WalletAccount[] _walletAccounts;

    private readonly string[] _testWifs =
    [
        "KxuRSsHgJMb3AMSN6B9P3JHNGMFtxmuimqgR9MmXPcv3CLLfusTd",
        "L2zWf8HxYmJCH4hXE97aLaDpppgEvyk19pwC4h8bP2z7gSSdRZy8",
        "L2zXV5idFea5x7j3LCNquUdWj2GkE9nG4TxbB2MbURRN6KtwhHzN",
        "KyyUEPaufpbFLLdHJNJhgx7X69ionpv8RG7MeNrQFQqQhD3agcXS",
        "L1XoPCmNhXAdGqfWKZ5Bo3k9DmJTa3wRaf2oYrcYE1tT6C2NJx5B",
        "L3HinHgao6N8YiXMxDDxbGsXQWu4x4v5kWcs5C7u8r1cLttdbQGj",
        "L5Sdps5JkWAAkpF2t8du5eNFYtEaBcJL5CWcvWBHuouuQP3PBEc9",
        "Kye4GjtsifdMyLN8KvWyEQgPgqvEwtwR6a2ePxZLacPcKJkBGpug",
        "L2SLACky8ZXQkm2bjS6fTVP5JipKzDLToAQK8dqQc3g2S3LLvLtB",
        "Kx6NX7mcCUG6VoTpV3SmojweHBoFwCUVJ3mVPjRy4fk9C3LP5pVg"
    ];

    public UT_DBFT() : base("akka.suppress-json-serializer-warning = on")
    {
        _walletProviders = new TestWalletProvider[7];
        _wallets = new NEP6Wallet[7];
        _walletAccounts = new WalletAccount[7];
        for (var i = 0; i < 7; i++)
        {
            _walletProviders[i] = new TestWalletProvider(_testWifs[i]);
            _wallets[i] = _walletProviders[i].GetWallet() as NEP6Wallet;
            _walletAccounts[i] = _wallets[i].GetAccounts().FirstOrDefault();
        }
        _memoryStore = new MemoryStore();
        SetupMockStorage();
        _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);

        _mockLocalNode = CreateTestProbe("mockLocalNode");
        _mockBlockchain = CreateTestProbe("mockBlockchain");
        _mockTaskManager = new Mock<IActorRef>();
        _txRouter = new Mock<IActorRef>();
        _testSettings = CreateTestSettings();

        _testValidators = CreateTestValidators();
        SetupMocks();
        SetupTaskManagerMock();
        SetupTxRouterMock();
        _neoSystem = new NeoSystem(TestProtocolSettings.Default, _memoryStoreProvider, _mockLocalNode.TestActor, _mockBlockchain.TestActor, _mockTaskManager.Object, _txRouter.Object);
        _consensusService = ActorOfAsTestActorRef<ConsensusService>(ConsensusService.Props(_neoSystem, _testSettings, _wallets[s_currentIndex]));
    }

    private ConsensusContext ConsensusContext
    {
        get
        {
            if (_consensusService is null) throw new NullReferenceException("_consensusService is null");
            return _consensusService.UnderlyingActor.context;
        }
    }
}
