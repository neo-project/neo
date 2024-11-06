// Copyright (C) 2015-2024 The Neo Project.
//
// ShowCommand.StateCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Service.Extensions;
using Neo.SmartContract.Native;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.CommandLine.Prompt
{
    internal partial class ShowCommand
    {
        public class StateCommand : Command
        {
            private readonly NeoSystem _neoSystem;
            private readonly LocalNode _localNode;

            public StateCommand(
                NeoSystem neoSystem) : base("state", "Show remote addresses and block height.")
            {
                _neoSystem = neoSystem;
                _localNode = _neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

                this.SetHandler(Invoke);
            }

            public void Invoke(InvocationContext context)
            {
                var cancel = CancellationTokenSource.CreateLinkedTokenSource(context.GetCancellationToken());

                context.Console.Write($"{Ansi.Cursor.Hide}");
                _ = ShowStateAsync(context.Console, cancel.Token);

                context.Console.ReadLine();
                context.Console.Write($"{Ansi.Cursor.Show}");
                cancel.Cancel();
            }

            private async Task ShowStateAsync(IConsole console, CancellationToken stoppingToken = default)
            {
                _ = PingRemoteClientsAsync(stoppingToken);

                console.Clear();

                while (stoppingToken.IsCancellationRequested == false)
                {

                    var height = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
                    var headerHeight = _neoSystem.HeaderCache.Last?.Index ?? height;
                    console.SetCursorPosition(0, 0);

                    console.WriteLine($"Block: {height}/{headerHeight} Connected: {_localNode.ConnectedCount} Unconnected: {Ansi.Color.Foreground.DarkGray}{_localNode.UnconnectedCount}");
                    foreach (var node in _localNode.GetRemoteNodes()
                        .OrderByDescending(u => u.LastBlockIndex)
                        .Take(Console.WindowHeight - 2)
                        .ToArray())
                    {
                        console.WriteLine($"{Ansi.Color.Foreground.Blue}IP: {Ansi.Color.Foreground.White}{node.Remote.Address,-15} {Ansi.Color.Foreground.Cyan}Port: {Ansi.Color.Foreground.White}{node.Remote.Port,-5} {Ansi.Color.Foreground.Blue}Listen: {Ansi.Color.Foreground.White}{node.ListenerTcpPort,-5} {Ansi.Color.Foreground.Cyan}Height: {Ansi.Color.Foreground.White}{node.LastBlockIndex,-7}");
                        console.ResetColor();
                    }

                    await Task.Delay(500, stoppingToken);
                }
            }

            private async Task PingRemoteClientsAsync(CancellationToken stoppingToken = default)
            {
                while (stoppingToken.IsCancellationRequested == false)
                {
                    _neoSystem.LocalNode.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView))));
                    await Task.Delay(15000, stoppingToken);
                }
            }
        }
    }
}
