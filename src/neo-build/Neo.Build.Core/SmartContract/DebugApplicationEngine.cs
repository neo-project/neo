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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;
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
        public IReadOnlyDictionary<Script, HashSet<uint>> BreakPoints => _breakPoints;

        private readonly Dictionary<Script, HashSet<uint>> _breakPoints = [];

        public void AddBreakPoints(Script script, params uint[] positions)
        {
            if (_breakPoints.TryGetValue(script, out var positionTable))
                positionTable.UnionWith(positions);
            else
            {
                positionTable = [];
                positionTable.UnionWith(positions);
                _breakPoints.Add(script, positionTable);
            }
        }

        public bool RemoveBreakPoints(Script script, params uint[] positions)
        {
            if (_breakPoints.TryGetValue(script, out var positionTable))
            {
                foreach (var position in positions)
                {
                    if (positionTable.Remove(position) == false)
                        throw new ArgumentException($"Position at {position} was not found.");
                }

                if (positionTable.Count == 0)
                    _breakPoints.Remove(script);

                return true;
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
    }
}
