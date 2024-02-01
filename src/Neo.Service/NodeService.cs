// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Service.Pipes;
using Neo.SmartContract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal sealed partial class NodeService : BackgroundService
    {
        public static NodeService? Instance { get; private set; }

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<NodeService> _logger;
        private readonly ProtocolSettings _nodeProtocolSettings;
        private readonly ApplicationSettings _appSettings;
        private readonly NamedPipeService _namedPipeService;

        private NeoSystem? _neoSystem;
        private LocalNode? _localNode;
        private Task? _importBlocksTask;
        private CancellationTokenSource? _importBlocksTokenSource;

        public NodeService(
            IConfiguration config,
            ILoggerFactory loggerFactory)
        {
            if (Instance is not null) throw new ApplicationException("Instance already running.");
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<NodeService>();
            _nodeProtocolSettings = ProtocolSettings.Load(config.GetRequiredSection("ProtocolConfiguration"));
            _appSettings = ApplicationSettings.Load(config.GetRequiredSection("ApplicationConfiguration"));
            _namedPipeService = new(_nodeProtocolSettings, loggerFactory);
            Instance = this;
            Utility.Logging += OnNeoUtilityLogging;
            Blockchain.Committed += OnNeoBlockchainCommitted;
            ApplicationEngine.Log += OnNeoApplicationEngineLog;
            ApplicationEngine.Notify += OnNeoApplicationEngineNotify;
        }

        public override void Dispose()
        {
            _namedPipeService.Dispose();
            Utility.Logging -= OnNeoUtilityLogging;
            Blockchain.Committed -= OnNeoBlockchainCommitted;
            ApplicationEngine.Log -= OnNeoApplicationEngineLog;
            ApplicationEngine.Notify -= OnNeoApplicationEngineNotify;
            Instance = null;
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CreateNeoSystemAsync(stoppingToken);
            _importBlocksTokenSource ??= CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _importBlocksTask = ImportThenStartNeoSystemAsync(_appSettings.Storage.Import.Verify, _importBlocksTokenSource.Token);

            await _namedPipeService.StartAsync(_appSettings.NamedPipe.Instances, stoppingToken); // Block Thread and Listen

            _logger.LogInformation("Shutting down...");
            await StopImportBlocksAsync();
            await StopNeoSystemAsync();
            _logger.LogInformation("Shutdown completed.");
        }
    }
}
