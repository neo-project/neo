// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.Import.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Logging;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Service.IO;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal partial class NodeService
    {
        private readonly Progress<double> _importProgress;
        private uint _importCounter;

        internal async Task StopImportBlocksAsync()
        {

            if (_importBlocksTask is not null)
            {
                _importBlocksTokenSource?.Cancel();
                await _importBlocksTask;
                _logger.LogInformation("Stopped importing blocks.");
            }

            _importBlocksTokenSource?.Dispose();
            _importBlocksTokenSource = null;
        }

        private async Task ImportThenStartNeoSystemAsync(bool verify, CancellationToken cancellationToken)
        {
            if (_neoSystem is null) return;
            if (_appSettings.Storage.Engine == nameof(MemoryStore))
            {
                await StartNeoSystemAsync(cancellationToken);
                return;
            }

            _logger.LogInformation("Started importing blocks.");

            var currentHeight = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);

            using var blocksBeingImported = BlockchainBackup.ReadBlocksFromAccFile(
                currentHeight, AppContext.BaseDirectory, _importProgress)
                .GetEnumerator();

            while (cancellationToken.IsCancellationRequested == false)
            {
                var blocksToImport = new List<Block>();
                for (var i = 0; i < 10; i++)
                {
                    if (blocksBeingImported.MoveNext() == false) break;
                    blocksToImport.Add(blocksBeingImported.Current);
                }
                if (blocksToImport.Count == 0) break;
                await _neoSystem.Blockchain.Ask<Blockchain.ImportCompleted>(new Blockchain.Import
                {
                    Blocks = blocksToImport,
                    Verify = verify
                }, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                _logger.LogInformation("Import blocks canceled!");
            else
                _logger.LogInformation("Import blocks finished!");

            await StartNeoSystemAsync(cancellationToken);
        }

        private void OnImportBlocksProgressChanged(object? sender, double e)
        {
            if (_importCounter++ % 50000 == 0)
                _logger.LogInformation("Importing blocks {Precent}% complete.", Math.Round(e, 2));
        }
    }
}
