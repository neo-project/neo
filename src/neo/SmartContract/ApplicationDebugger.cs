// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;

namespace Neo.SmartContract
{
    public class ApplicationDebugger : ApplicationEngine
    {
        protected ApplicationDebugger(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, ProtocolSettings settings, long gas, IDiagnostic diagnostic) : base(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic)
        {
        }

        public static new ApplicationDebugger Create(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock = null, ProtocolSettings settings = null, long gas = TestModeGas, IDiagnostic diagnostic = null)
        {
            return new ApplicationDebugger(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic);
        }

        public new VMState State
        {
            get
            {
                return base.State;
            }
            set
            {
                base.State = value;
            }
        }

        public new void ExecuteNext()
        {
            base.ExecuteNext();
        }
    }
}
