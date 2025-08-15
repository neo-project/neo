// Copyright (C) 2015-2025 The Neo Project.
//
// DebugApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.SmartContract.Debugger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System.Collections.Generic;

namespace Neo.Build.Core.SmartContract
{
    public partial class DebugApplicationEngine : ApplicationEngineBase
    {
        public DebugApplicationEngine(
            ProtocolSettings protocolSettings,
            DataCache snapshotCache,
            ApplicationEngineSettings? engineSettings = null,
            TriggerType trigger = TriggerType.Application,
            IVerifiable? container = null,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null,
            ILoggerFactory? loggerFactory = null,
            IReadOnlyDictionary<uint, InteropDescriptor>? systemCallMethods = null)
            : base(
                  protocolSettings,
                  snapshotCache,
                  engineSettings ?? new(),
                  trigger,
                  container,
                  persistingBlock,
                  diagnostic,
                  loggerFactory,
                  systemCallMethods)
        { }

        /// <summary>
        /// All the set breakpoints for a given <see cref="Script"/>.
        /// </summary>
        public IReadOnlyDictionary<Breakpoint, HashSet<uint>> BreakPoints => _breakPoints;

        /// <summary>
        /// Gets state storage events of the keys and values that were either read or updated.
        /// </summary>
        public IReadOnlyDictionary<ExecutionContextState, DebugStorage> SnapshotStack => _snapshotStack;

        private readonly Dictionary<Breakpoint, HashSet<uint>> _breakPoints = [];
        private readonly Dictionary<ExecutionContextState, DebugStorage> _snapshotStack = [];

        public void AddBreakPoints(Script script, uint? blockIndex, UInt256? txHash, uint position)
        {
            var bp = Breakpoint.Create(script, blockIndex, txHash);

            if (_breakPoints.TryGetValue(bp, out var positionTable))
                positionTable.Add(position);
            else
            {
                positionTable = [position];
                _breakPoints.Add(bp, positionTable);
            }
        }

        public bool RemoveBreakPoints(Script script, uint? blockIndex, UInt256? txHash, uint position)
        {
            var bp = Breakpoint.Create(script, blockIndex, txHash);

            if (_breakPoints.TryGetValue(bp, out var positionTable))
            {
                var ret = positionTable.Remove(position);

                if (positionTable.Count == 0)
                    ret = _breakPoints.Remove(bp);

                return ret;
            }

            return false;
        }

        public override VMState Execute()
        {
            if (State == VMState.BREAK)
                State = VMState.NONE;

            while (State == VMState.NONE)
            {
                ExecuteNext();
                CheckBreakPointsAndBreak();
            }

            return State;
        }

        public VMState StepInto()
        {
            if (State == VMState.HALT || State == VMState.FAULT)
                return State;

            ExecuteNext();

            if (State == VMState.NONE)
                State = VMState.BREAK;

            return State;
        }

        public VMState StepOut()
        {
            if (State == VMState.BREAK)
                State = VMState.NONE;

            var stackCount = InvocationStack.Count;

            while (State == VMState.NONE && InvocationStack.Count >= stackCount)
            {
                ExecuteNext();
                CheckBreakPointsAndBreak();
            }

            if (State == VMState.NONE)
                State = VMState.BREAK;

            return State;
        }

        public VMState StepOver()
        {
            if (State == VMState.HALT || State == VMState.FAULT)
                return State;

            State = VMState.NONE;

            var stackCount = InvocationStack.Count;

            do
            {
                ExecuteNext();
                CheckBreakPointsAndBreak();
            } while (State == VMState.NONE && InvocationStack.Count > stackCount);

            if (State == VMState.NONE)
                State = VMState.BREAK;

            return State;
        }

        public override void LoadContext(ExecutionContext context)
        {
            var exeState = context.GetState<ExecutionContextState>();

            exeState.SnapshotCache.OnRead += OnReadSnapshot;
            exeState.SnapshotCache.OnUpdate += OnUpdateSnapshotCache;

            base.LoadContext(context);
        }
    }
}
