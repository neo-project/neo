// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo.Build.Core.Factories;
using Neo.Build.Core.Logging;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Build.Core.SmartContract
{
    public abstract partial class ApplicationEngineBase : ApplicationEngine
    {
        protected ApplicationEngineBase(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            long maxGas = 20_00000000L,
            StorageSettings? storageSettings = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            ILoggerFactory? loggerFactory = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : base(
                  trigger,
                  container,
                  snapshotCache,
                  persistingBlock,
                  protocolSettings,
                  maxGas,
                  diagnostic,
                  DefaultJumpTable)
        {
            _systemCallMethods = systemCallMethods ?? ApplicationEngineFactory.SystemCallBaseServices;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _traceLogger = _loggerFactory.CreateLogger(nameof(ApplicationEngine));
            _storageSettings = storageSettings ?? new();
        }

        protected ApplicationEngineBase(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            ApplicationEngineSettings engineSettings,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            ILoggerFactory? loggerFactory = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : this(
                protocolSettings,
                snapshotCache,
                engineSettings.MaxGas,
                engineSettings.Storage,
                trigger,
                container,
                persistingBlock,
                diagnostic,
                loggerFactory,
                systemCallMethods)
        { }

        public Transaction? CurrentTransaction => ScriptContainer as Transaction;
        protected ILogger Logger => _traceLogger;

        private readonly IReadOnlyDictionary<uint, InteropDescriptor> _systemCallMethods;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _traceLogger;

        private readonly Encoding _encoding = Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        private readonly StorageSettings _storageSettings;

        public override VMState Execute()
        {
            ReadOnlyMemory<byte> memoryScript = CurrentContext?.Script ?? ReadOnlyMemory<byte>.Empty;
            var scriptString = System.Convert.ToBase64String(memoryScript.Span);

            _traceLogger.LogDebug(DebugEventLog.Execute,
                "Executing Container=\"{TxHash}\" Script=\"{Script}\"",
                ScriptContainer?.Hash, scriptString);

            var result = base.Execute();

            _traceLogger.LogDebug(DebugEventLog.Execute,
                "Executed State=\"{VMState}\" Gas=\"{Consumed}\" GasLeft=\"{GasLeft}\" Result=\"{Result}\"",
                result, FeeConsumed, GasLeft, ResultStack.ToJson());

            return result;
        }

        public override void LoadContext(ExecutionContext context)
        {
            base.LoadContext(context);

            var contextState = context.GetState<ExecutionContextState>();
            var contractState = contextState.Contract;

            contextState.SnapshotCache.OnRead += OnSnapshotCacheRead;
            contextState.SnapshotCache.OnUpdate += OnSnapshotCacheUpdate;

            if (contextState.ScriptHash is not null &&
                contractState is not null)
                _traceLogger.LogDebug(DebugEventLog.Load,
                    "Loaded Name=\"{Name}\" Hash=\"{ScriptHash}\"",
                    contractState.Manifest.Name, contextState.ScriptHash);
            else
            {
                ReadOnlyMemory<byte> memoryScript = context.Script ?? ReadOnlyMemory<byte>.Empty;
                var scriptString = System.Convert.ToBase64String(memoryScript.Span);

                _traceLogger.LogDebug(DebugEventLog.Load,
                    "Loaded Script=\"{Script}\"",
                    scriptString);
            }
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);

            _traceLogger.LogError(DebugEventLog.Fault, ex, string.Empty);
        }

        protected override void OnSysCall(InteropDescriptor descriptor)
        {
            if (_systemCallMethods.TryGetValue(descriptor, out var overrideDescriptor))
                base.OnSysCall(overrideDescriptor);
            else
                base.OnSysCall(descriptor);
        }

        private void OnSnapshotCacheRead(DataCache sender, StorageKey key, StorageItem item)
        {
            var keyString = GetStorageKeyValueString(key.ToArray(), _storageSettings.KeyFormat);
            var valueString = GetStorageKeyValueString(item.ToArray(), _storageSettings.ValueFormat);

            _traceLogger.LogDebug(DebugEventLog.ReadStorage,
                "Storage Id=\"{Id}\" Key=\"{Key}\" Value=\"{Value}\"",
                key.Id, keyString, valueString);
        }

        private void OnSnapshotCacheUpdate(DataCache sender, StorageKey key, StorageItem item)
        {
            var keyString = GetStorageKeyValueString(key.ToArray(), _storageSettings.KeyFormat);
            var valueString = GetStorageKeyValueString(item.ToArray(), _storageSettings.ValueFormat);

            _traceLogger.LogDebug(DebugEventLog.UpdateStorage,
                "Storage Id=\"{Id}\" Key=\"{Key}\" Value=\"{Value}\"",
                key.Id, keyString, valueString);
        }
    }
}
