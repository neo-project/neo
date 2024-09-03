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

            _ = PingRemoteClientsAsync(stoppingToken);

            _console.Clear();

            while (stoppingToken.IsCancellationRequested == false)
            {
                var height = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
                var headerHeight = _neoSystem.HeaderCache.Last?.Index ?? height;

                _console.SetCursorPosition(0, 0);

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

        public async Task PingRemoteClientsAsync(CancellationToken stoppingToken = default)
        {
            if (_neoSystem is null)
                throw new InvalidOperationException($"{nameof(NeoSystemHostedService)} has not been started.");

            while (stoppingToken.IsCancellationRequested == false)
            {
                _neoSystem.LocalNode.Tell(Message.Create(MessageCommand.Ping, PingPayload.Create(NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView))));
                await Task.Delay(_protocolSettings.TimePerBlock, stoppingToken);
            }
        }

        public void ShowBlock(string indexOrHash)
        {
            if (_neoSystem is null)
                throw new InvalidOperationException($"{nameof(NeoSystemHostedService)} has not been started.");

            Block? block = null;

            if (uint.TryParse(indexOrHash, out var index))
                block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, index);
            else if (UInt256.TryParse(indexOrHash, out var hash))
                block = NativeContract.Ledger.GetBlock(_neoSystem.StoreView, hash);
            else
            {
                _console.ErrorMessage("Enter a valid block index or hash.");
                return;
            }

            if (block is null)
            {
                _console.ErrorMessage($"Block {indexOrHash} doesn't exist.");
                return;
            }

            DateTime blockDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            blockDatetime = blockDatetime.AddMilliseconds(block.Timestamp).ToLocalTime();

            _console.WriteLine("-------------Block-------------");
            _console.WriteLine();
            _console.WriteLine($"      Timestamp: {blockDatetime}");
            _console.WriteLine($"          Index: {block.Index}");
            _console.WriteLine($"           Hash: {block.Hash}");
            _console.WriteLine($"          Nonce: {block.Nonce}");
            _console.WriteLine($"     MerkleRoot: {block.MerkleRoot}");
            _console.WriteLine($"       PrevHash: {block.PrevHash}");
            _console.WriteLine($"  NextConsensus: {block.NextConsensus}");
            _console.WriteLine($"   PrimaryIndex: {block.PrimaryIndex}");
            _console.WriteLine($"  PrimaryPubKey: {NativeContract.NEO.GetCommittee(_neoSystem.StoreView)[block.PrimaryIndex]}");
            _console.WriteLine($"        Version: {block.Version}");
            _console.WriteLine($"           Size: {block.Size} Byte(s)");
            _console.WriteLine();

            _console.WriteLine("-------------Witness-------------");
            _console.WriteLine();
            _console.WriteLine($"    Invocation Script: {Convert.ToBase64String(block.Witness.InvocationScript.Span)}");
            _console.WriteLine($"  Verification Script: {Convert.ToBase64String(block.Witness.VerificationScript.Span)}");
            _console.WriteLine($"           ScriptHash: {block.Witness.ScriptHash}");
            _console.WriteLine($"                 Size: {block.Witness.Size} Byte(s)");
            _console.WriteLine();

            _console.WriteLine("------------Transaction(s)------------");
            _console.WriteLine();

            if (block.Transactions.Length == 0)
            {
                _console.WriteLine("  No Transaction(s)");
            }
            else
            {
                foreach (var tx in block.Transactions)
                    _console.WriteLine($"  {tx.Hash}");
            }
            _console.WriteLine();
            _console.WriteLine("--------------------------------------");
        }
    }
}
