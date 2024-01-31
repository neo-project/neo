// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.Loggers.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Linq;

namespace Neo.Service
{
    internal partial class NodeService
    {
        private void OnNeoApplicationEngineNotify(object? sender, NotifyEventArgs e)
        {
            var logger = NodeUtilities.CreateOrGetLogger(_loggerFactory, $"{nameof(Blockchain)}.Contracts.{e.ScriptHash}.Events.{e.EventName}");
            var contract = NativeContract.Contracts.SingleOrDefault(s => s.Hash == e.ScriptHash);

            logger.LogDebug("name=\"{Name}\", hash={ScriptHash}, event=\"{EventName}\", state={State}, tx={TxHash}",
                contract?.Manifest.Name, e.ScriptHash, e.EventName, e.State.ToJson(), e.ScriptContainer?.Hash);
        }

        private void OnNeoApplicationEngineLog(object? sender, LogEventArgs e)
        {
            var logger = NodeUtilities.CreateOrGetLogger(_loggerFactory, $"{nameof(Blockchain)}.Contracts.{e.ScriptHash}.Logs");
            var contract = NativeContract.Contracts.SingleOrDefault(s => s.Hash == e.ScriptHash);

            logger.LogDebug("name=\"{Name}\", hash={ScriptHash}, text=\"{Message}\", tx={TxHash}",
                contract?.Manifest.Name, e.ScriptHash, e.Message, e.ScriptContainer?.Hash);
        }

        private void OnNeoBlockchainCommitted(NeoSystem system, Network.P2P.Payloads.Block block)
        {
            if (block.Index == 0) return;

            var blockJsonLogger = _loggerFactory.CreateLogger($"{nameof(Blockchain)}.Blocks.{block.Hash}");
            blockJsonLogger.LogDebug("{Json}", block.Header.ToJson(_nodeProtocolSettings));

            foreach (var tx in block.Transactions)
            {
                var txJsonLogger = _loggerFactory.CreateLogger($"{nameof(Blockchain)}.Transactions.{tx.Hash}");
                txJsonLogger.LogDebug("{Json}", tx.ToJson(_nodeProtocolSettings));
            }
        }

        private void OnNeoUtilityLogging(string source, LogLevel level, object message)
        {
            var neoLogger = NodeUtilities.CreateOrGetLogger(_loggerFactory, source);

            switch (level)
            {
                case LogLevel.Debug:
                    neoLogger.LogDebug("{Message}", message);
                    break;
                case LogLevel.Info:
                    neoLogger.LogInformation("{Message}", message);
                    break;
                case LogLevel.Warning:
                    neoLogger.LogWarning("{Message}", message);
                    break;
                case LogLevel.Error:
                    neoLogger.LogError("{Message}", message);
                    break;
                case LogLevel.Fatal:
                    neoLogger.LogCritical("{Message}", message);
                    break;
                default:
                    neoLogger.LogTrace("{Message}", message);
                    break;
            }
        }
    }
}
