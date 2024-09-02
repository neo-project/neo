// Copyright (C) 2015-2024 The Neo Project.
//
// NeoSystemHostedService.Methods.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.CLI.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Hosting.Services
{
    internal partial class NeoSystemHostedService
    {
        public async Task ShowStateAsync(CancellationToken stoppingToken = default)
        {
            if (_neoSystem is null || _localNode is null)
                throw new InvalidOperationException($"{nameof(NeoSystemHostedService)} has not been started.");

            _ = PingAllClientsAsync(stoppingToken);

            while (stoppingToken.IsCancellationRequested == false)
            {
                uint height = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
                uint headerHeight = _neoSystem.HeaderCache.Last?.Index ?? height;

                _console.SetCursorPosition(0, 0);
                Console.CursorVisible = false;

                _console.WriteLine($"Block: {height}/{headerHeight} Connected: {_localNode.ConnectedCount} Unconnected: {_localNode.UnconnectedCount}");
                foreach (var node in _localNode.GetRemoteNodes()
                    .OrderByDescending(u => u.LastBlockIndex)
                    .Take(Console.WindowHeight - 2)
                    .ToArray())
                {
                    _console.WriteLine($"IP: {node.Remote.Address,-15} Port: {node.Remote.Port,-5} Listen: {node.ListenerTcpPort,-5} Height: {node.LastBlockIndex,-7}");
                }

                await Task.Delay(500, stoppingToken);
            }
        }

        public async Task PingAllClientsAsync(CancellationToken stoppingToken = default)
        {
            if (_neoSystem is null)
                throw new InvalidOperationException($"{nameof(NeoSystemHostedService)} has not been started.");

            while (stoppingToken.IsCancellationRequested == false)
            {
                _neoSystem.LocalNode.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView))));
                await Task.Delay(_protocolSettings.TimePerBlock, stoppingToken);
            }
        }
    }
}
