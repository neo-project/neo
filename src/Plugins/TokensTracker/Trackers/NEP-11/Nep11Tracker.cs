// Copyright (C) 2015-2024 The Neo Project.
//
// Nep11Tracker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Plugins.Trackers.NEP_11
{
    class Nep11Tracker : TrackerBase
    {
        private const byte Nep11BalancePrefix = 0xf8;
        private const byte Nep11TransferSentPrefix = 0xf9;
        private const byte Nep11TransferReceivedPrefix = 0xfa;
        private uint _currentHeight;
        private Block _currentBlock;
        private readonly HashSet<string> _properties = new()
        {
            "name",
            "description",
            "image",
            "tokenURI"
        };

        public override string TrackName => nameof(Nep11Tracker);

        public Nep11Tracker(IStore db, uint maxResult, bool shouldRecordHistory, NeoSystem system) : base(db, maxResult, shouldRecordHistory, system)
        {
        }

        public override void OnPersist(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            _currentBlock = block;
            _currentHeight = block.Index;
            uint nep11TransferIndex = 0;
            List<TransferRecord> transfers = new();
            foreach (Blockchain.ApplicationExecuted appExecuted in applicationExecutedList)
            {
                // Executions that fault won't modify storage, so we can skip them.
                if (appExecuted.VMState.HasFlag(VMState.FAULT)) continue;
                foreach (var notifyEventArgs in appExecuted.Notifications)
                {
                    if (notifyEventArgs.EventName != "Transfer" || notifyEventArgs?.State is not Array stateItems ||
                        stateItems.Count == 0)
                        continue;
                    var contract = NativeContract.ContractManagement.GetContract(snapshot, notifyEventArgs.ScriptHash);
                    if (contract?.Manifest.SupportedStandards.Contains("NEP-11") == true)
                    {
                        try
                        {
                            HandleNotificationNep11(notifyEventArgs.ScriptContainer, notifyEventArgs.ScriptHash, stateItems, transfers, ref nep11TransferIndex);
                        }
                        catch (Exception e)
                        {
                            Log(e.ToString(), LogLevel.Error);
                            throw;
                        }
                    }

                }
            }

            // update nep11 balance
            var contracts = new Dictionary<UInt160, (bool isDivisible, ContractState state)>();
            foreach (var transferRecord in transfers)
            {
                if (!contracts.ContainsKey(transferRecord.asset))
                {
                    var state = NativeContract.ContractManagement.GetContract(snapshot, transferRecord.asset);
                    var balanceMethod = state.Manifest.Abi.GetMethod("balanceOf", 1);
                    var balanceMethod2 = state.Manifest.Abi.GetMethod("balanceOf", 2);
                    if (balanceMethod == null && balanceMethod2 == null)
                    {
                        Log($"{state.Hash} is not nft!", LogLevel.Warning);
                        continue;
                    }

                    var isDivisible = balanceMethod2 != null;
                    contracts[transferRecord.asset] = (isDivisible, state);
                }

                var asset = contracts[transferRecord.asset];
                if (asset.isDivisible)
                {
                    SaveDivisibleNFTBalance(transferRecord, snapshot);
                }
                else
                {
                    SaveNFTBalance(transferRecord);
                }
            }
        }

        private void SaveDivisibleNFTBalance(TransferRecord record, DataCache snapshot)
        {
            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(record.asset, "balanceOf", record.from, record.tokenId);
            sb.EmitDynamicCall(record.asset, "balanceOf", record.to, record.tokenId);
            using ApplicationEngine engine = ApplicationEngine.Run(sb.ToArray(), snapshot, settings: _neoSystem.Settings, gas: 3400_0000);
            if (engine.State.HasFlag(VMState.FAULT) || engine.ResultStack.Count != 2)
            {
                Log($"Fault: from[{record.from}] to[{record.to}] get {record.asset} token [{record.tokenId.ToHexString()}] balance fault", LogLevel.Warning);
                return;
            }
            var toBalance = engine.ResultStack.Pop();
            var fromBalance = engine.ResultStack.Pop();
            if (toBalance is not Integer || fromBalance is not Integer)
            {
                Log($"Fault: from[{record.from}] to[{record.to}] get {record.asset} token [{record.tokenId.ToHexString()}] balance not number", LogLevel.Warning);
                return;
            }
            Put(Nep11BalancePrefix, new Nep11BalanceKey(record.to, record.asset, record.tokenId), new TokenBalance { Balance = toBalance.GetInteger(), LastUpdatedBlock = _currentHeight });
            Put(Nep11BalancePrefix, new Nep11BalanceKey(record.from, record.asset, record.tokenId), new TokenBalance { Balance = fromBalance.GetInteger(), LastUpdatedBlock = _currentHeight });
        }

        private void SaveNFTBalance(TransferRecord record)
        {
            if (record.from != UInt160.Zero)
            {
                Delete(Nep11BalancePrefix, new Nep11BalanceKey(record.from, record.asset, record.tokenId));
            }

            if (record.to != UInt160.Zero)
            {
                Put(Nep11BalancePrefix, new Nep11BalanceKey(record.to, record.asset, record.tokenId), new TokenBalance { Balance = 1, LastUpdatedBlock = _currentHeight });
            }
        }


        private void HandleNotificationNep11(IVerifiable scriptContainer, UInt160 asset, Array stateItems, List<TransferRecord> transfers, ref uint transferIndex)
        {
            if (stateItems.Count != 4) return;
            var transferRecord = GetTransferRecord(asset, stateItems);
            if (transferRecord == null) return;

            transfers.Add(transferRecord);
            if (scriptContainer is Transaction transaction)
            {
                RecordTransferHistoryNep11(asset, transferRecord.from, transferRecord.to, transferRecord.tokenId, transferRecord.amount, transaction.Hash, ref transferIndex);
            }
        }


        private void RecordTransferHistoryNep11(UInt160 contractHash, UInt160 from, UInt160 to, ByteString tokenId, BigInteger amount, UInt256 txHash, ref uint transferIndex)
        {
            if (!_shouldTrackHistory) return;
            if (from != UInt160.Zero)
            {
                Put(Nep11TransferSentPrefix,
                    new Nep11TransferKey(from, _currentBlock.Header.Timestamp, contractHash, tokenId, transferIndex),
                    new TokenTransfer
                    {
                        Amount = amount,
                        UserScriptHash = to,
                        BlockIndex = _currentHeight,
                        TxHash = txHash
                    });
            }

            if (to != UInt160.Zero)
            {
                Put(Nep11TransferReceivedPrefix,
                    new Nep11TransferKey(to, _currentBlock.Header.Timestamp, contractHash, tokenId, transferIndex),
                    new TokenTransfer
                    {
                        Amount = amount,
                        UserScriptHash = from,
                        BlockIndex = _currentHeight,
                        TxHash = txHash
                    });
            }
            transferIndex++;
        }


        [RpcMethod]
        public JToken GetNep11Transfers(JArray _params)
        {
            _shouldTrackHistory.True_Or(RpcError.MethodNotFound);
            UInt160 userScriptHash = GetScriptHashFromParam(_params[0].AsString());
            // If start time not present, default to 1 week of history.
            ulong startTime = _params.Count > 1 ? (ulong)_params[1].AsNumber() :
                (DateTime.UtcNow - TimeSpan.FromDays(7)).ToTimestampMS();
            ulong endTime = _params.Count > 2 ? (ulong)_params[2].AsNumber() : DateTime.UtcNow.ToTimestampMS();
            (endTime >= startTime).True_Or(RpcError.InvalidParams);

            JObject json = new();
            json["address"] = userScriptHash.ToAddress(_neoSystem.Settings.AddressVersion);
            JArray transfersSent = new();
            json["sent"] = transfersSent;
            JArray transfersReceived = new();
            json["received"] = transfersReceived;
            AddNep11Transfers(Nep11TransferSentPrefix, userScriptHash, startTime, endTime, transfersSent);
            AddNep11Transfers(Nep11TransferReceivedPrefix, userScriptHash, startTime, endTime, transfersReceived);
            return json;
        }

        [RpcMethod]
        public JToken GetNep11Balances(JArray _params)
        {
            UInt160 userScriptHash = GetScriptHashFromParam(_params[0].AsString());

            JObject json = new();
            JArray balances = new();
            json["address"] = userScriptHash.ToAddress(_neoSystem.Settings.AddressVersion);
            json["balance"] = balances;

            var map = new Dictionary<UInt160, List<(string tokenid, BigInteger amount, uint height)>>();
            int count = 0;
            byte[] prefix = Key(Nep11BalancePrefix, userScriptHash);
            foreach (var (key, value) in _db.FindPrefix<Nep11BalanceKey, TokenBalance>(prefix))
            {
                if (NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, key.AssetScriptHash) is null)
                    continue;
                if (!map.TryGetValue(key.AssetScriptHash, out var list))
                {
                    map[key.AssetScriptHash] = list = new List<(string, BigInteger, uint)>();
                }
                list.Add((key.Token.GetSpan().ToHexString(), value.Balance, value.LastUpdatedBlock));
                count++;
                if (count >= _maxResults)
                {
                    break;
                }
            }
            foreach (var key in map.Keys)
            {
                try
                {
                    using var script = new ScriptBuilder();
                    script.EmitDynamicCall(key, "decimals");
                    script.EmitDynamicCall(key, "symbol");

                    var engine = ApplicationEngine.Run(script.ToArray(), _neoSystem.StoreView, settings: _neoSystem.Settings);
                    var symbol = engine.ResultStack.Pop().GetString();
                    var decimals = engine.ResultStack.Pop().GetInteger();
                    var name = NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, key).Manifest.Name;

                    balances.Add(new JObject
                    {
                        ["assethash"] = key.ToString(),
                        ["name"] = name,
                        ["symbol"] = symbol,
                        ["decimals"] = decimals.ToString(),
                        ["tokens"] = new JArray(map[key].Select(v => new JObject
                        {
                            ["tokenid"] = v.tokenid,
                            ["amount"] = v.amount.ToString(),
                            ["lastupdatedblock"] = v.height
                        })),
                    });
                }
                catch { }
            }
            return json;
        }

        [RpcMethod]
        public JToken GetNep11Properties(JArray _params)
        {
            UInt160 nep11Hash = GetScriptHashFromParam(_params[0].AsString());
            var tokenId = _params[1].AsString().HexToBytes();

            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(nep11Hash, "properties", CallFlags.ReadOnly, tokenId);
            using var snapshot = _neoSystem.GetSnapshot();

            using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, settings: _neoSystem.Settings);
            JObject json = new();

            if (engine.State == VMState.HALT)
            {
                var map = engine.ResultStack.Pop<Map>();
                foreach (var keyValue in map)
                {
                    if (keyValue.Value is CompoundType) continue;
                    var key = keyValue.Key.GetString();
                    if (_properties.Contains(key))
                    {
                        json[key] = keyValue.Value.GetString();
                    }
                    else
                    {
                        json[key] = keyValue.Value.IsNull ? null : keyValue.Value.GetSpan().ToBase64();
                    }
                }
            }
            return json;
        }

        private void AddNep11Transfers(byte dbPrefix, UInt160 userScriptHash, ulong startTime, ulong endTime, JArray parentJArray)
        {
            var transferPairs = QueryTransfers<Nep11TransferKey, TokenTransfer>(dbPrefix, userScriptHash, startTime, endTime).Take((int)_maxResults).ToList();
            foreach (var (key, value) in transferPairs.OrderByDescending(l => l.key.TimestampMS))
            {
                JObject transfer = ToJson(key, value);
                transfer["tokenid"] = key.Token.GetSpan().ToHexString();
                parentJArray.Add(transfer);
            }
        }
    }
}
