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
            TriggerType trigger,
            IVerifiable container,
            DataCache snapshotCache,
            Block persistingBlock,
            ProtocolSettings settings,
            long gas,
            IDiagnostic? diagnostic = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null) : base(trigger, container, snapshotCache, persistingBlock, settings, gas, diagnostic, DefaultJumpTable)
        {
            _orgSysCall = DefaultJumpTable[OpCode.SYSCALL];
            DefaultJumpTable[OpCode.SYSCALL] = OnSystemCall;
            _systemCallMethods = systemCallMethods ?? ApplicationEngineDefaults.SystemCallBaseServices;
        }

        protected ApplicationEngineBase(
            TriggerType trigger,
            IVerifiable container,
            DataCache snapshotCache,
            Block persistingBlock,
            NeoBuildSettings settings,
            IDiagnostic? diagnostic = null) : this(trigger, container, snapshotCache, persistingBlock, settings.ProtocolSettings, gas, diagnostic, null)
        {

        }

        private readonly JumpTable.DelAction _orgSysCall;
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

        protected void OnSystemCall(ExecutionEngine engine, Instruction instruction)
        {
            var systemCallMethodPointer = instruction.TokenU32;

            if (_systemCallMethods.TryGetValue(systemCallMethodPointer, out var descriptor))
                OnSysCall(descriptor);
            else
                _orgSysCall(engine, instruction);
        }
    }
}
