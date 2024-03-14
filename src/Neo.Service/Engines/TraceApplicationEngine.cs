// Copyright (C) 2015-2024 The Neo Project.
//
// TraceApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;

namespace Neo.Service.Engines
{
    internal sealed partial class TraceApplicationEngine : ApplicationEngine
    {
        public TraceApplicationEngine(
            TriggerType trigger,
            IVerifiable container,
            DataCache snapshot,
            Block? persistingBlock,
            ProtocolSettings? protocolSettings,
            long gas,
            IDiagnostic? diagnostic,
            JumpTable jumpTable,
            ILoggerFactory? loggerFactory = null) : base(trigger, container, snapshot, persistingBlock, protocolSettings, gas, diagnostic)
        {
            if (loggerFactory is not null)
                _logger = NodeUtilities.CreateOrGetLogger(loggerFactory, GenerateLoggerCategoryName(protocolSettings, persistingBlock, container));
            Log += OnLog;
        }

        public override void Dispose()
        {
            _postExecutedInstructions.Clear();
            Log -= OnLog;
            base.Dispose();
            _logger?.LogTrace("{TraceLog}", _traceLog.ToString());
            GC.SuppressFinalize(this);
        }

        public override VMState Execute()
        {
            if (CurrentContext?.LocalVariables is not null &&
                CurrentContext?.StaticFields is not null &&
                CurrentContext?.Arguments is not null)
            {
                _traceLog.AppendLine($"---Start {nameof(CurrentContext)}---");
                TraceLog(CurrentContext.StaticFields, "SF");
                TraceLog(CurrentContext.LocalVariables, "LV");
                TraceLog(CurrentContext.Arguments, "AR");
                _traceLog.AppendLine($"---End {nameof(CurrentContext)}---");
            }
            _traceLog.AppendLine($"---Start {nameof(Execute)}---");
            var stateResult = base.Execute();
            _traceLog.AppendLine($"---End {nameof(Execute)}---");
            _traceLog.AppendLine($"---Start {nameof(ResultStack)}---");
            _traceLog.AppendLine($"{ResultStack.ToJson()}");
            _traceLog.AppendLine($"---End {nameof(ResultStack)}---");
            return stateResult;
        }

        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);
            TraceLog(instruction);
            _postExecutedInstructions.Enqueue(instruction);
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);

            var exception = ex.InnerException ?? ex;

            _traceLog.AppendLine($"---Exception [{ex.GetType().Name}]:\"{ex.Message}\"---");
        }
    }
}
