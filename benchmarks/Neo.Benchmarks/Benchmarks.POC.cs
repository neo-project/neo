// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.POC.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using System.Diagnostics;

namespace Neo.Benchmark
{
    public class Benchmarks_PoCs
    {
        private static readonly ProtocolSettings protocol = ProtocolSettings.Load("config.json");
        private static readonly NeoSystem system = new(protocol, (string)null);

        [Benchmark]
        public void NeoIssue2725()
        {
            // https://github.com/neo-project/neo/issues/2725
            // L00: INITSSLOT 1
            // L01: NEWARRAY0
            // L02: PUSHDATA1 6161616161 //"aaaaa"
            // L03: PUSHINT16 500
            // L04: STSFLD0
            // L05: OVER
            // L06: OVER
            // L07: SYSCALL 95016f61 //System.Runtime.Notify
            // L08: LDSFLD0
            // L09: DEC
            // L10: DUP
            // L11: STSFLD0
            // L12: JMPIF L05
            // L13: CLEAR
            // L14: SYSCALL dbfea874 //System.Runtime.GetExecutingScriptHash
            // L15: PUSHINT16 8000
            // L16: STSFLD0
            // L17: DUP
            // L18: SYSCALL 274335f1 //System.Runtime.GetNotifications
            // L19: DROP
            // L20: LDSFLD0
            // L21: DEC
            // L22: DUP
            // L23: STSFLD0
            // L24: JMPIF L17
            Run(nameof(NeoIssue2725), "VgHCDAVhYWFhYQH0AWBLS0GVAW9hWJ1KYCT1SUHb/qh0AUAfYEpBJ0M18UVYnUpgJPU=");
        }

        private static void Run(string name, string poc)
        {
            Random random = new();
            Transaction tx = new()
            {
                Version = 0,
                Nonce = (uint)random.Next(),
                SystemFee = 20_00000000,
                NetworkFee = 1_00000000,
                ValidUntilBlock = ProtocolSettings.Default.MaxTraceableBlocks,
                Signers = Array.Empty<Signer>(),
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = Convert.FromBase64String(poc),
                Witnesses = Array.Empty<Witness>()
            };
            using var snapshot = system.GetSnapshotCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, system.GenesisBlock, protocol, tx.SystemFee);
            engine.LoadScript(tx.Script);
            engine.Execute();
            Debug.Assert(engine.State == VMState.FAULT);
        }
    }
}
