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

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.Build.Core.SmartContract
{
    public abstract partial class ApplicationEngineBase : ApplicationEngine
    {
        protected ApplicationEngineBase(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            long maxGas,
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
            _systemCallMethods = systemCallMethods ?? ApplicationEngineDefaults.SystemCallBaseServices;
        }

        protected ApplicationEngineBase(
            ApplicationEngineSettings engineSettings,
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : this(
                protocolSettings,
                snapshotCache,
                engineSettings.MaxGas,
                trigger,
                container,
                persistingBlock,
                diagnostic,
                systemCallMethods)
        { }

        protected ApplicationEngineBase(
            NeoBuildSettings settings,
            DataCache snapshotCache,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : this(
                settings.ApplicationEngineSettings,
                settings.ProtocolSettings,
                snapshotCache,
                trigger,
                container,
                persistingBlock,
                diagnostic,
                systemCallMethods)
        { }

        private readonly IReadOnlyDictionary<uint, InteropDescriptor> _systemCallMethods;

        public override void Dispose()
        {
            base.Dispose();
        }

        public override VMState Execute()
        {
            return base.Execute();
        }

        public override void LoadContext(ExecutionContext context)
        {
            base.LoadContext(context);
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);
        }

        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);
        }

        protected override void PreExecuteInstruction(Instruction instruction)
        {
            base.PreExecuteInstruction(instruction);
        }

        protected override void OnSysCall(InteropDescriptor descriptor)
        {
            if (_systemCallMethods.TryGetValue(descriptor, out var overrideDescriptor))
                base.OnSysCall(overrideDescriptor);
            else
                base.OnSysCall(descriptor);
        }

        protected virtual void OnLog(object? sender, LogEventArgs e)
        {

        }

        protected virtual void OnNotify(object? sender, NotifyEventArgs e)
        {

        }
    }
}
