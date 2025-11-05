// Copyright (C) 2015-2025 The Neo Project.
//
// RpcServer.Node.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins.RpcServer
{
    partial class RpcServer
    {

        /// <summary>
        /// Gets the current number of connections to the node.
        /// <para>Request format:</para>
        /// <code>{ "jsonrpc": "2.0", "id": 1,"method": "getconnectioncount"}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": 10}</code>
        /// </summary>
        /// <returns>The number of connections as a JToken.</returns>
        [RpcMethod]
        protected internal virtual JsonNode GetConnectionCount()
        {
            return localNode.ConnectedCount;
        }

        /// <summary>
        /// Gets information about the peers connected to the node.
        /// <para>Request format:</para>
        /// <code>{ "jsonrpc": "2.0", "id": 1,"method": "getpeers"}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "unconnected": [{"address": "The address", "port": "The port"}],
        ///     "bad": [],
        ///     "connected": [{"address": "The address", "port": "The port"}]
        ///   }
        /// }</code>
        /// </summary>
        /// <returns>A JObject containing information about unconnected, bad, and connected peers.</returns>
        [RpcMethod]
        protected internal virtual JsonNode GetPeers()
        {
            return new JsonObject()
            {
                ["unconnected"] = new JsonArray(localNode.GetUnconnectedPeers().Select(p =>
                {
                    return new JsonObject() { ["address"] = p.Address.ToString(), ["port"] = p.Port, };
                }).ToArray()),
                ["bad"] = new JsonArray(),
                ["connected"] = new JsonArray(localNode.GetRemoteNodes().Select(p =>
                {
                    return new JsonObject() { ["address"] = p.Remote.Address.ToString(), ["port"] = p.ListenerTcpPort, };
                }).ToArray())
            };
        }

        /// <summary>
        /// Processes the result of a transaction or block relay and returns appropriate response or throws an exception.
        /// </summary>
        /// <param name="reason">The verification result of the relay.</param>
        /// <param name="hash">The hash of the transaction or block.</param>
        /// <returns>A JObject containing the hash if successful, otherwise throws an RpcException.</returns>
        private static JsonObject GetRelayResult(VerifyResult reason, UInt256 hash)
        {
            switch (reason)
            {
                case VerifyResult.Succeed:
                    return new JsonObject() { ["hash"] = hash.ToString() };
                case VerifyResult.AlreadyExists:
                    throw new RpcException(RpcError.AlreadyExists.WithData(reason.ToString()));
                case VerifyResult.AlreadyInPool:
                    throw new RpcException(RpcError.AlreadyInPool.WithData(reason.ToString()));
                case VerifyResult.OutOfMemory:
                    throw new RpcException(RpcError.MempoolCapReached.WithData(reason.ToString()));
                case VerifyResult.InvalidScript:
                    throw new RpcException(RpcError.InvalidScript.WithData(reason.ToString()));
                case VerifyResult.InvalidAttribute:
                    throw new RpcException(RpcError.InvalidAttribute.WithData(reason.ToString()));
                case VerifyResult.InvalidSignature:
                    throw new RpcException(RpcError.InvalidSignature.WithData(reason.ToString()));
                case VerifyResult.OverSize:
                    throw new RpcException(RpcError.InvalidSize.WithData(reason.ToString()));
                case VerifyResult.Expired:
                    throw new RpcException(RpcError.ExpiredTransaction.WithData(reason.ToString()));
                case VerifyResult.InsufficientFunds:
                    throw new RpcException(RpcError.InsufficientFunds.WithData(reason.ToString()));
                case VerifyResult.PolicyFail:
                    throw new RpcException(RpcError.PolicyFailed.WithData(reason.ToString()));
                default:
                    throw new RpcException(RpcError.VerificationFailed.WithData(reason.ToString()));

            }
        }

        /// <summary>
        /// Gets version information about the node, including network, protocol, and RPC settings.
        /// <para>Request format:</para>
        /// <code>{ "jsonrpc": "2.0", "id": 1,"method": "getversion"}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{
        ///   "jsonrpc": "2.0",
        ///   "id": 1,
        ///   "result": {
        ///     "tcpport": 10333, // The TCP port,
        ///     "nonce": 1, // The nonce,
        ///     "useragent": "The user agent",
        ///     "rpc": {
        ///       "maxiteratorresultitems": 100, // The maximum number of items in the iterator result,
        ///       "sessionenabled": false // Whether session is enabled,
        ///      },
        ///      "protocol": {
        ///       "addressversion": 0x35, // The address version,
        ///       "network": 5195086, // The network,
        ///       "validatorscount": 0, // The number of validators,
        ///       "msperblock": 15000, // The time per block in milliseconds,
        ///       "maxtraceableblocks": 2102400, // The maximum traceable blocks,
        ///       "maxvaliduntilblockincrement": 86400000 / 15000, // The maximum valid until block increment,
        ///       "maxtransactionsperblock": 512, // The maximum number of transactions per block,
        ///       "memorypoolmaxtransactions": 50000, // The maximum number of transactions in the memory pool,
        ///       "initialgasdistribution": 5200000000000000, // The initial gas distribution,
        ///       "hardforks": [{"name": "The hardfork name", "blockheight": 0/*The block height*/ }],
        ///       "standbycommittee": ["The public key"],
        ///       "seedlist": ["The seed list"]
        ///     }
        ///   }
        /// }</code>
        /// </summary>
        /// <returns>A JObject containing detailed version and configuration information.</returns>
        [RpcMethod]
        protected internal virtual JsonNode GetVersion()
        {
            JsonObject json = new()
            {
                ["tcpport"] = localNode.ListenerTcpPort,
                ["nonce"] = LocalNode.Nonce,
                ["useragent"] = LocalNode.UserAgent
            };
            // rpc settings
            JsonObject rpc = new()
            {
                ["maxiteratorresultitems"] = settings.MaxIteratorResultItems,
                ["sessionenabled"] = settings.SessionEnabled
            };
            // protocol settings
            JsonObject protocol = new()
            {
                ["addressversion"] = system.Settings.AddressVersion,
                ["network"] = system.Settings.Network,
                ["validatorscount"] = system.Settings.ValidatorsCount,
                ["msperblock"] = system.GetTimePerBlock().TotalMilliseconds,
                ["maxtraceableblocks"] = system.GetMaxTraceableBlocks(),
                ["maxvaliduntilblockincrement"] = system.GetMaxValidUntilBlockIncrement(),
                ["maxtransactionsperblock"] = system.Settings.MaxTransactionsPerBlock,
                ["memorypoolmaxtransactions"] = system.Settings.MemoryPoolMaxTransactions,
                ["initialgasdistribution"] = system.Settings.InitialGasDistribution,
                ["hardforks"] = new JsonArray(system.Settings.Hardforks.Select(hf => new JsonObject()
                {
                    // Strip "HF_" prefix.
                    ["name"] = StripPrefix(hf.Key.ToString(), "HF_"),
                    ["blockheight"] = hf.Value
                }).ToArray()),
                ["standbycommittee"] = new JsonArray(system.Settings.StandbyCommittee.Select(u => (JsonNode)u.ToString()).ToArray()),
                ["seedlist"] = new JsonArray(system.Settings.SeedList.Select(u => (JsonNode)u).ToArray())
            };
            json["rpc"] = rpc;
            json["protocol"] = protocol;
            return json;
        }

        /// <summary>
        /// Removes a specified prefix from a string if it exists.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <param name="prefix">The prefix to remove.</param>
        /// <returns>The string with the prefix removed if it existed, otherwise the original string.</returns>
        private static string StripPrefix(string s, string prefix)
        {
            return s.StartsWith(prefix) ? s.Substring(prefix.Length) : s;
        }

        /// <summary>
        /// Sends a raw transaction to the network.
        /// <para>Request format:</para>
        /// <code>
        /// {"jsonrpc": "2.0", "id": 1,"method": "sendrawtransaction", "params": ["A Base64-encoded transaction"]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": {"hash": "The hash of the transaction(UInt256)"}}</code>
        /// </summary>
        /// <param name="base64Tx">The base64-encoded transaction.</param>
        /// <returns>A JToken containing the result of the transaction relay.</returns>
        [RpcMethod]
        protected internal virtual JsonNode SendRawTransaction(string base64Tx)
        {
            var tx = Result.Ok_Or(
                () => Convert.FromBase64String(base64Tx).AsSerializable<Transaction>(),
                RpcError.InvalidParams.WithData($"Invalid Transaction Format: {base64Tx}"));
            var reason = system.Blockchain.Ask<RelayResult>(tx).Result;
            return GetRelayResult(reason.Result, tx.Hash);
        }

        /// <summary>
        /// Submits a new block to the network.
        /// <para>Request format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1,"method": "submitblock", "params": ["A Base64-encoded block"]}
        /// </code>
        /// <para>Response format:</para>
        /// <code>{"jsonrpc": "2.0", "id": 1, "result": {"hash": "The hash of the block(UInt256)"}}</code>
        /// </summary>
        /// <param name="base64Block">The base64-encoded block.</param>
        /// <returns>A JToken containing the result of the block submission.</returns>
        [RpcMethod]
        protected internal virtual JsonNode SubmitBlock(string base64Block)
        {
            var block = Result.Ok_Or(
                () => Convert.FromBase64String(base64Block).AsSerializable<Block>(),
                RpcError.InvalidParams.WithData($"Invalid Block Format: {base64Block}"));
            var reason = system.Blockchain.Ask<RelayResult>(block).Result;
            return GetRelayResult(reason.Result, block.Hash);
        }
    }
}
