// Copyright (C) 2015-2025 The Neo Project.
//
// ScriptHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;

namespace Neo.Plugins.RestServer.Helpers
{
    internal static class ScriptHelper
    {
        public static bool InvokeMethod(ProtocolSettings protocolSettings, DataCache snapshot, UInt160 scriptHash, string method, out StackItem[] results, params object[] args)
        {
            using var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitDynamicCall(scriptHash, method, CallFlags.ReadOnly, args);
            byte[] script = scriptBuilder.ToArray();
            using var engine = ApplicationEngine.Run(script, snapshot, settings: protocolSettings, gas: RestServerSettings.Current.MaxGasInvoke);
            results = engine.State == VMState.FAULT ? [] : [.. engine.ResultStack];
            return engine.State == VMState.HALT;
        }

        public static ApplicationEngine InvokeMethod(ProtocolSettings protocolSettings, DataCache snapshot, UInt160 scriptHash, string method, ContractParameter[] args, Signer[]? signers, out byte[] script)
        {
            using var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitDynamicCall(scriptHash, method, CallFlags.All, args);
            script = scriptBuilder.ToArray();
            var tx = signers == null ? null : new Transaction
            {
                Version = 0,
                Nonce = (uint)Random.Shared.Next(),
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + protocolSettings.MaxValidUntilBlockIncrement,
                Signers = signers,
                Attributes = [],
                Script = script,
                Witnesses = [.. signers.Select(s => new Witness())],
            };
            using var engine = ApplicationEngine.Run(script, snapshot, tx, settings: protocolSettings, gas: RestServerSettings.Current.MaxGasInvoke);
            return engine;
        }

        public static ApplicationEngine InvokeScript(ReadOnlyMemory<byte> script, Signer[]? signers = null, Witness[]? witnesses = null)
        {
            var neoSystem = RestServerPlugin.NeoSystem ?? throw new InvalidOperationException();

            var snapshot = neoSystem.GetSnapshotCache();
            var tx = signers == null ? null : new Transaction
            {
                Version = 0,
                Nonce = (uint)Random.Shared.Next(),
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + neoSystem.Settings.MaxValidUntilBlockIncrement,
                Signers = signers,
                Attributes = [],
                Script = script,
                Witnesses = witnesses
            };
            return ApplicationEngine.Run(script, snapshot, tx, settings: neoSystem.Settings, gas: RestServerSettings.Current.MaxGasInvoke);
        }
    }
}
