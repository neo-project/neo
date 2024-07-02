// Copyright (C) 2015-2024 The Neo Project.
//
// NEP17Token.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.Plugins.RestServer.Helpers;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.Plugins.RestServer.Tokens
{
    internal class NEP17Token
    {
        public UInt160 ScriptHash { get; private init; }
        public string Name { get; private init; } = string.Empty;
        public string Symbol { get; private init; } = string.Empty;
        public byte Decimals { get; private init; }

        private readonly NeoSystem _neosystem;
        private readonly DataCache _datacache;

        public NEP17Token(
            NeoSystem neoSystem,
            UInt160 scriptHash,
            DataCache? snapshot = null)
        {
            _datacache = snapshot ?? neoSystem.GetSnapshot();
            var contractState = NativeContract.ContractManagement.GetContract(_datacache, scriptHash) ?? throw new ArgumentException(null, nameof(scriptHash));
            if (ContractHelper.IsNep17Supported(contractState) == false)
                throw new NotSupportedException(nameof(scriptHash));
            byte[] script;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitDynamicCall(scriptHash, "decimals", CallFlags.ReadOnly);
                sb.EmitDynamicCall(scriptHash, "symbol", CallFlags.ReadOnly);
                script = sb.ToArray();
            }
            using var engine = ApplicationEngine.Run(script, _datacache, settings: neoSystem.Settings, gas: RestServerSettings.Current.MaxGasInvoke);
            if (engine.State != VMState.HALT)
                throw new NotSupportedException(nameof(scriptHash));

            _neosystem = neoSystem;
            ScriptHash = scriptHash;
            Name = contractState.Manifest.Name;
            Symbol = engine.ResultStack.Pop().GetString() ?? string.Empty;
            Decimals = (byte)engine.ResultStack.Pop().GetInteger();
        }

        public BigDecimal BalanceOf(UInt160 address)
        {
            if (ContractHelper.GetContractMethod(_datacache, ScriptHash, "balanceOf", 1) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            if (ScriptHelper.InvokeMethod(_neosystem.Settings, _datacache, ScriptHash, "balanceOf", out var result, address))
                return new BigDecimal(result[0].GetInteger(), Decimals);
            return new BigDecimal(BigInteger.Zero, Decimals);
        }

        public BigDecimal TotalSupply()
        {
            if (ContractHelper.GetContractMethod(_datacache, ScriptHash, "totalSupply", 0) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            if (ScriptHelper.InvokeMethod(_neosystem.Settings, _datacache, ScriptHash, "totalSupply", out var result))
                return new BigDecimal(result[0].GetInteger(), Decimals);
            return new BigDecimal(BigInteger.Zero, Decimals);
        }
    }
}
