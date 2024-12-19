// Copyright (C) 2015-2024 The Neo Project.
//
// ModelExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P;
using Neo.Plugins.RestServer.Models;
using Neo.Plugins.RestServer.Models.Error;
using Neo.Plugins.RestServer.Models.Node;
using Neo.Plugins.RestServer.Models.Token;
using Neo.Plugins.RestServer.Tokens;
using Neo.SmartContract;
using System;
using System.Linq;

namespace Neo.Plugins.RestServer.Extensions
{
    internal static class ModelExtensions
    {
        public static ExecutionEngineModel ToModel(this ApplicationEngine ae) =>
            new()
            {
                GasConsumed = ae.FeeConsumed,
                State = ae.State,
                Notifications = ae.Notifications.Select(s =>
                    new BlockchainEventModel()
                    {
                        ScriptHash = s.ScriptHash,
                        EventName = s.EventName,
                        State = [.. s.State],
                    }).ToArray(),
                ResultStack = [.. ae.ResultStack],
                FaultException = ae.FaultException == null ?
                    null :
                    new ErrorModel()
                    {
                        Code = ae.FaultException?.InnerException?.HResult ?? ae.FaultException?.HResult ?? -1,
                        Name = ae.FaultException?.InnerException?.GetType().Name ?? ae.FaultException?.GetType().Name ?? string.Empty,
                        Message = ae.FaultException?.InnerException?.Message ?? ae.FaultException?.Message ?? string.Empty,
                    },
            };

        public static NEP17TokenModel ToModel(this NEP17Token token) =>
            new()
            {
                Name = token.Name,
                Symbol = token.Symbol,
                ScriptHash = token.ScriptHash,
                Decimals = token.Decimals,
                TotalSupply = token.TotalSupply().Value,
            };

        public static NEP11TokenModel ToModel(this NEP11Token nep11) =>
            new()
            {
                Name = nep11.Name,
                ScriptHash = nep11.ScriptHash,
                Symbol = nep11.Symbol,
                Decimals = nep11.Decimals,
                TotalSupply = nep11.TotalSupply().Value,
                Tokens = nep11.Tokens().Select(s => new
                {
                    Key = s,
                    Value = nep11.Properties(s),
                }).ToDictionary(key => Convert.ToHexString(key.Key), value => value.Value),
            };

        public static ProtocolSettingsModel ToModel(this ProtocolSettings protocolSettings) =>
            new()
            {
                Network = protocolSettings.Network,
                AddressVersion = protocolSettings.AddressVersion,
                ValidatorsCount = protocolSettings.ValidatorsCount,
                MillisecondsPerBlock = protocolSettings.MillisecondsPerBlock,
                MaxValidUntilBlockIncrement = protocolSettings.MaxValidUntilBlockIncrement,
                MaxTransactionsPerBlock = protocolSettings.MaxTransactionsPerBlock,
                MemoryPoolMaxTransactions = protocolSettings.MemoryPoolMaxTransactions,
                MaxTraceableBlocks = protocolSettings.MaxTraceableBlocks,
                InitialGasDistribution = protocolSettings.InitialGasDistribution,
                SeedList = protocolSettings.SeedList,
                Hardforks = protocolSettings.Hardforks.ToDictionary(k => k.Key.ToString().Replace("HF_", string.Empty), v => v.Value),
                StandbyValidators = protocolSettings.StandbyValidators,
                StandbyCommittee = protocolSettings.StandbyCommittee,
            };

        public static RemoteNodeModel ToModel(this RemoteNode remoteNode) =>
            new()
            {
                RemoteAddress = remoteNode.Remote.Address.ToString(),
                RemotePort = remoteNode.Remote.Port,
                ListenTcpPort = remoteNode.ListenerTcpPort,
                LastBlockIndex = remoteNode.LastBlockIndex,
            };
    }
}
