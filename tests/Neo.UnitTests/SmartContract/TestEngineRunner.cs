// Copyright (C) 2015-2026 The Neo Project.
//
// TestEngineRunner.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Shared helpers for ApplicationEngine coverage tests.
    /// </summary>
    internal static class TestEngineRunner
    {
        public static ApplicationEngine Create(
            DataCache snapshot,
            IVerifiable container = null,
            TriggerType trigger = TriggerType.Application,
            long gas = 100_0000_0000,
            ProtocolSettings settings = null)
        {
            return ApplicationEngine.Create(
                trigger,
                container,
                snapshot,
                settings: settings ?? TestProtocolSettings.Default,
                gas: gas);
        }

        public static ApplicationEngine CreateWithScript(
            DataCache snapshot,
            ReadOnlyMemory<byte> script,
            IVerifiable container = null,
            long gas = 100_0000_0000)
        {
            var engine = Create(snapshot, container, gas: gas);
            engine.LoadScript(script);
            return engine;
        }

        public static Transaction EmptyTx(UInt160 account)
        {
            return new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = [],
                Signers = [new Signer { Account = account, Scopes = WitnessScope.CalledByEntry }],
                Script = new byte[] { (byte)OpCode.RET },
                Witnesses = []
            };
        }
    }
}
