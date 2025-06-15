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
using Neo.Build.Core.SmartContract.Debugger;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ApplicationEngineSettings engineSettings,
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
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

        private readonly IReadOnlyDictionary<uint, InteropDescriptor> _systemCallMethods;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _traceLogger;

        private readonly Encoding _encoding = Encoding.GetEncoding("UTF-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        private readonly StorageSettings _storageSettings;
        private readonly ApplicationEngineDebugSink _engineDebugSink = new();

        public override void Dispose()
        {
            base.Dispose();
        }

        public override VMState Execute()
        {
            ReadOnlyMemory<byte> memoryScript = CurrentContext?.Script ?? ReadOnlyMemory<byte>.Empty;
            var scriptString = System.Convert.ToBase64String(memoryScript.Span);

            _traceLogger.LogInformation(DebugEventLog.Execute,
                "Executing container={TxHash}, script={Script}",
                ScriptContainer?.Hash, scriptString);

            _engineDebugSink.Script = memoryScript;
            _engineDebugSink.ScriptContainer = ScriptContainer;

            var result = base.Execute();

            _engineDebugSink.State = result;
            _engineDebugSink.GasFee = FeeConsumed;
            _engineDebugSink.GasLeft = GasLeft;
            _engineDebugSink.Results = ResultStack;

            var callSink = new DebugCallSink(nameof(Execute), DebugEventLog.Execute,
                [System.Convert.ToBase64String(memoryScript.Span)], ResultStack);
            AddDebugSinkCallStack(callSink);

            _traceLogger.LogInformation(DebugEventLog.Execute,
                "Executed state={VMState}, gas={Consumed}, gasleft={GasLeft}, result={Result}",
                result, FeeConsumed, GasLeft, ResultStack.ToJson());

            return result;
        }

        public override void LoadContext(ExecutionContext context)
        {
            base.LoadContext(context);

            var contextState = context.GetState<ExecutionContextState>();
            var contractState = contextState.Contract;

            if (contextState.ScriptHash is not null &&
                contractState is not null)
            {
                var storageItems = SnapshotCache
                    .Find(StorageKey.CreateSearchPrefix(contractState.Id, default))
                    .Select(s => new DebugStorage(s.Key, s.Value));
                var contractSink = new DebugContractSink(contractState, storageItems);
                AddDebugSinkContractStack(contractSink);

                _traceLogger.LogInformation(DebugEventLog.Load,
                    "Loaded name={Name}, hash={ScriptHash}",
                    contractState.Manifest.Name, contextState.ScriptHash);

            }
            else
            {
                ReadOnlyMemory<byte> memoryScript = context.Script ?? ReadOnlyMemory<byte>.Empty;
                var scriptString = System.Convert.ToBase64String(memoryScript.Span);

                var callSink = new DebugCallSink(nameof(LoadContext), DebugEventLog.Load,
                    [$"{context.GetScriptHash()}", $"{scriptString}",]);
                AddDebugSinkCallStack(callSink);

                _traceLogger.LogInformation(DebugEventLog.Load,
                    "Loaded script={Script}",
                    scriptString);
            }
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);

            var callSink = new DebugCallSink(nameof(OnFault), DebugEventLog.Fault,
                [$"{ex.InnerException?.Message ?? ex.Message}"], ResultStack);
            AddDebugSinkCallStack(callSink);

            _traceLogger.LogError(DebugEventLog.Fault, ex, string.Empty);

            foreach (var call in _engineDebugSink.CallStack)
                _traceLogger.LogDebug(DebugEventLog.Log,
                    "callstack={CallStack}", call);
        }

        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);

            if (State == VMState.HALT || State == VMState.FAULT)
            {
                var callSink = new DebugCallSink(nameof(PostExecuteInstruction), DebugEventLog.Post,
                    [$"{instruction.OpCode}"]);
                AddDebugSinkCallStack(callSink);
            }

            var contextState = CurrentContext?.GetState<ExecutionContextState>();
            var contractState = contextState?.Contract;

            if (contextState?.ScriptHash is not null &&
                contractState is not null)
            {
                var storageItems = SnapshotCache
                    .Find(StorageKey.CreateSearchPrefix(contractState.Id, default))
                    .Select(s => new DebugStorage(s.Key, s.Value));
                var contractSink = new DebugContractSink(contractState, storageItems);
                AddDebugSinkPostContractStack(contractSink);
            }
        }

        protected override void PreExecuteInstruction(Instruction instruction)
        {
            base.PreExecuteInstruction(instruction);

            var callSink = new DebugCallSink(nameof(PreExecuteInstruction), DebugEventLog.PrePost,
                [$"{instruction.OpCode}"]);
            AddDebugSinkCallStack(callSink);
        }

        protected override void OnSysCall(InteropDescriptor descriptor)
        {
            if (_systemCallMethods.TryGetValue(descriptor, out var overrideDescriptor))
                base.OnSysCall(overrideDescriptor);
            else
                base.OnSysCall(descriptor);
        }
    }
}
