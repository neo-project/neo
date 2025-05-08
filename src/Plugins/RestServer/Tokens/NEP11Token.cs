// Copyright (C) 2015-2025 The Neo Project.
//
// NEP11Token.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Persistence;
using Neo.Plugins.RestServer.Helpers;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.Plugins.RestServer.Tokens
{
    internal class NEP11Token
    {
        public UInt160 ScriptHash { get; private set; }
        public string Name { get; private set; }
        public string Symbol { get; private set; }
        public byte Decimals { get; private set; }

        private readonly NeoSystem _neoSystem;
        private readonly DataCache _snapshot;
        private readonly ContractState _contract;

        public NEP11Token(
            NeoSystem neoSystem,
            UInt160 scriptHash) : this(neoSystem, null, scriptHash) { }

        public NEP11Token(
            NeoSystem neoSystem,
            DataCache? snapshot,
            UInt160 scriptHash)
        {
            ArgumentNullException.ThrowIfNull(neoSystem, nameof(neoSystem));
            ArgumentNullException.ThrowIfNull(scriptHash, nameof(scriptHash));
            _neoSystem = neoSystem;
            _snapshot = snapshot ?? _neoSystem.GetSnapshotCache();
            _contract = NativeContract.ContractManagement.GetContract(_snapshot, scriptHash) ?? throw new ArgumentException(null, nameof(scriptHash));
            if (ContractHelper.IsNep11Supported(_contract) == false)
                throw new NotSupportedException(nameof(scriptHash));
            Name = _contract.Manifest.Name;
            ScriptHash = scriptHash;

            byte[] scriptBytes;
            using var sb = new ScriptBuilder();
            sb.EmitDynamicCall(_contract.Hash, "decimals", CallFlags.ReadOnly);
            sb.EmitDynamicCall(_contract.Hash, "symbol", CallFlags.ReadOnly);
            scriptBytes = sb.ToArray();

            using var appEngine = ApplicationEngine.Run(scriptBytes, _snapshot, settings: _neoSystem.Settings, gas: RestServerSettings.Current.MaxGasInvoke);
            if (appEngine.State != VMState.HALT)
                throw new NotSupportedException(nameof(ScriptHash));

            Symbol = appEngine.ResultStack.Pop().GetString() ?? throw new ArgumentNullException();
            Decimals = (byte)appEngine.ResultStack.Pop().GetInteger();
        }

        public BigDecimal TotalSupply()
        {
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "totalSupply", 0) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "totalSupply", out var results))
                return new(results[0].GetInteger(), Decimals);
            return new(BigInteger.Zero, Decimals);
        }

        public BigDecimal BalanceOf(UInt160 owner)
        {
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "balanceOf", 1) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "balanceOf", out var results, owner))
                return new(results[0].GetInteger(), Decimals);
            return new(BigInteger.Zero, Decimals);
        }

        public BigDecimal BalanceOf(UInt160 owner, byte[] tokenId)
        {
            if (Decimals == 0)
                throw new InvalidOperationException();
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "balanceOf", 2) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            ArgumentNullException.ThrowIfNull(tokenId, nameof(tokenId));
            if (tokenId.Length > 64)
                throw new ArgumentOutOfRangeException(nameof(tokenId));
            if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "balanceOf", out var results, owner, tokenId))
                return new(results[0].GetInteger(), Decimals);
            return new(BigInteger.Zero, Decimals);
        }

        public IEnumerable<byte[]> TokensOf(UInt160 owner)
        {
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "tokensOf", 1) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "tokensOf", out var results, owner))
            {
                if (results[0].GetInterface<object>() is IIterator iterator)
                {
                    var refCounter = new ReferenceCounter();
                    while (iterator.Next())
                        yield return iterator.Value(refCounter).GetSpan().ToArray();
                }
            }
        }

        public UInt160[] OwnerOf(byte[] tokenId)
        {
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "ownerOf", 1) == null)
                throw new NotSupportedException(nameof(ScriptHash));
            ArgumentNullException.ThrowIfNull(tokenId, nameof(tokenId));
            if (tokenId.Length > 64)
                throw new ArgumentOutOfRangeException(nameof(tokenId));
            if (Decimals == 0)
            {
                if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "ownerOf", out var results, tokenId))
                    return new[] { new UInt160(results[0].GetSpan()) };
            }
            else if (Decimals > 0)
            {
                if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "ownerOf", out var results, tokenId))
                {
                    if (results[0].GetInterface<object>() is IIterator iterator)
                    {
                        var refCounter = new ReferenceCounter();
                        var lstOwners = new List<UInt160>();
                        while (iterator.Next())
                            lstOwners.Add(new UInt160(iterator.Value(refCounter).GetSpan()));
                        return lstOwners.ToArray();
                    }
                }
            }
            return System.Array.Empty<UInt160>();
        }

        public IEnumerable<byte[]> Tokens()
        {
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "tokens", 0) == null)
                throw new NotImplementedException();
            if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "tokens", out var results))
            {
                if (results[0].GetInterface<object>() is IIterator iterator)
                {
                    var refCounter = new ReferenceCounter();
                    while (iterator.Next())
                        yield return iterator.Value(refCounter).GetSpan().ToArray();
                }
            }
        }

        public IReadOnlyDictionary<string, StackItem>? Properties(byte[] tokenId)
        {
            ArgumentNullException.ThrowIfNull(tokenId, nameof(tokenId));
            if (ContractHelper.GetContractMethod(_snapshot, ScriptHash, "properties", 1) == null)
                throw new NotImplementedException();
            if (tokenId.Length > 64)
                throw new ArgumentOutOfRangeException(nameof(tokenId));
            if (ScriptHelper.InvokeMethod(_neoSystem.Settings, _snapshot, ScriptHash, "properties", out var results, tokenId))
            {
                if (results[0] is Map map)
                {
                    return map.ToDictionary(key => key.Key.GetString() ?? throw new ArgumentNullException(), value => value.Value);
                }
            }
            return default;
        }
    }
}
