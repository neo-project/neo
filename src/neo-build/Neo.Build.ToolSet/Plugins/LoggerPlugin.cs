// Copyright (C) 2015-2025 The Neo Project.
//
// LoggerPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Factories;
using Neo.Build.ToolSet.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace Neo.Build.ToolSet.Plugins
{
    internal class LoggerPlugin : Plugin
    {
        private NeoSystem? _neoSystem;

        private readonly IConsole _console;

        public LoggerPlugin(
            IConsole console)
        {
            _console = console;

            Blockchain.Committing += OnBlockchainCommitting;
            ApplicationEngine.InstanceHandler += OnInstanceHandler;
            Utility.Logging += OnUtilityLogging;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            _neoSystem = system;
            base.OnSystemLoaded(system);
        }

        private void OnBlockchainCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            foreach (var app in applicationExecutedList)
                OnApplicationExecuted(app);
        }

        private void OnInstanceHandler(ApplicationEngine engine)
        {
            engine.Log += OnLog;
        }

        private void OnLog(ApplicationEngine engine, LogEventArgs args)
        {
            var container = args.ScriptContainer is null ?
                string.Empty :
                $"[{args.ScriptContainer.GetType().Name}]";
            var contractName = FunctionFactory.GetContractName(_neoSystem!.StoreView, args.ScriptHash);

            _console.WriteLine("[{0},{1}] [Log] \"{2}\"", contractName, container, args.Message);
        }

        private void OnUtilityLogging(string source, LogLevel level, object message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    _console.DebugMessage("[{0:HH:mm:ss.ff}] [{1}] \"{2}\"", DateTimeOffset.Now, source, message);
                    break;
                case LogLevel.Info:
                    _console.InfoMessage("[{0:HH:mm:ss.ff}] [{1}] \"{2}\"", DateTimeOffset.Now, source, message);
                    break;
                case LogLevel.Warning:
                    _console.WarningMessage("[{0:HH:mm:ss.ff}] [{1}] \"{2}\"", DateTimeOffset.Now, source, message);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    _console.ErrorMessage("[{0:HH:mm:ss.ff}] [{1}] \"{2}\"", DateTimeOffset.Now, source, message);
                    break;
            }
        }

        private void OnApplicationExecuted(Blockchain.ApplicationExecuted applicationExecuted)
        {
            if (applicationExecuted.VMState == VMState.FAULT)
            {
                var logMessage = string.Format("Tx Fault: hash={0}", applicationExecuted.Transaction.Hash);
                if (string.IsNullOrEmpty(applicationExecuted.Exception.Message) == false)
                    logMessage += string.Format(" exception={0}",
                        applicationExecuted.Exception.InnerException?.Message ??
                        applicationExecuted.Exception.Message);
                _console.ErrorMessage("{0}", logMessage);
            }
        }
    }
}
