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
            long maxGas,
            StorageSettings? storageSettings = null,
            ILoggerFactory? loggerFactory = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
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
            _orgSysCall = DefaultJumpTable[OpCode.SYSCALL];
            DefaultJumpTable[OpCode.SYSCALL] = OnSystemCall;
            _systemCallMethods = systemCallMethods ?? ApplicationEngineDefaults.SystemCallBaseServices;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _traceLogger = _loggerFactory.CreateLogger(nameof(ApplicationEngine));
            _storageSettings = storageSettings ?? new();
        }

        protected ApplicationEngineBase(
            ApplicationEngineSettings engineSettings,
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            ILoggerFactory? loggerFactory = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : this(
                protocolSettings,
                snapshotCache,
                engineSettings.MaxGas,
                engineSettings.Storage,
                loggerFactory,
                trigger,
                container,
                persistingBlock,
                diagnostic,
                systemCallMethods)
        { }

        protected ApplicationEngineBase(
            NeoBuildSettings settings,
            DataCache snapshotCache,
            ILoggerFactory? loggerFactory = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : this(
                settings.ApplicationEngineSettings,
                settings.ProtocolSettings,
                snapshotCache,
                loggerFactory,
                trigger,
                container,
                persistingBlock,
                diagnostic,
                systemCallMethods)
        { }

        public Transaction? CurrentTransaction => ScriptContainer as Transaction;

        private readonly JumpTable.DelAction _orgSysCall;
        private readonly IReadOnlyDictionary<uint, InteropDescriptor> _systemCallMethods;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _traceLogger;

        private readonly UTF8Encoding _encoding = new(false, true);

        private readonly StorageSettings _storageSettings;

        public override void Dispose()
        {
            base.Dispose();
        }

        public override VMState Execute()
        {
            _traceLogger.LogInformation(VMEventLog.Execute,
                "Executing container={TxHash}, script={Script}",
                ScriptContainer.Hash, CurrentTransaction?.Script);

            var result = base.Execute();

            _traceLogger.LogInformation(VMEventLog.Execute,
                "Executed state={VMState}, gas={Consumed}, leftover={GasLeft}, result={Result}",
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
                _traceLogger.LogInformation(VMEventLog.Load,
                    "Loaded name={Name}, hash={ScriptHash}",
                    contractState.Manifest.Name, contextState.ScriptHash);
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);

            _traceLogger.LogError(VMEventLog.Fault, ex,
                "{Message}",
                ex.InnerException?.Message ?? ex.Message);
        }

        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);
        }

        protected override void PreExecuteInstruction(Instruction instruction)
        {
            base.PreExecuteInstruction(instruction);
        }

        protected virtual void OnSystemCall(ExecutionEngine engine, Instruction instruction)
        {
            var systemCallMethodPointer = instruction.TokenU32;

            if (_systemCallMethods.TryGetValue(systemCallMethodPointer, out var descriptor))
                OnSysCall(descriptor);
            else
                _orgSysCall(engine, instruction);
        }
    }
}
