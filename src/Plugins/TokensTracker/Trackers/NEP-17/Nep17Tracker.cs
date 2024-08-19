// Copyright (C) 2015-2024 The Neo Project.
//
// Nep17Tracker.cs file belongs to the neo project and is free
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

namespace Neo.Plugins.Trackers.NEP_17
{
    record BalanceChangeRecord(UInt160 User, UInt160 Asset);

    class Nep17Tracker : TrackerBase
    {
        private const byte Nep17BalancePrefix = 0xe8;
        private const byte Nep17TransferSentPrefix = 0xe9;
        private const byte Nep17TransferReceivedPrefix = 0xea;
        private uint _currentHeight;
        private Block _currentBlock;

        public override string TrackName => nameof(Nep17Tracker);

        public Nep17Tracker(IStore db, uint maxResult, bool shouldRecordHistory, NeoSystem system) : base(db, maxResult, shouldRecordHistory, system)
        {
        }

        public override void OnPersist(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            _currentBlock = block;
            _currentHeight = block.Index;
            uint nep17TransferIndex = 0;
            var balanceChangeRecords = new HashSet<BalanceChangeRecord>();

            foreach (Blockchain.ApplicationExecuted appExecuted in applicationExecutedList)
            {
                // Executions that fault won't modify storage, so we can skip them.
                if (appExecuted.VMState.HasFlag(VMState.FAULT)) continue;
                foreach (var notifyEventArgs in appExecuted.Notifications)
                {
                    if (notifyEventArgs.EventName != "Transfer" || notifyEventArgs?.State is not Array stateItems || stateItems.Count == 0)
                        continue;
                    var contract = NativeContract.ContractManagement.GetContract(snapshot, notifyEventArgs.ScriptHash);
                    if (contract?.Manifest.SupportedStandards.Contains("NEP-17") == true)
                    {
                        try
                        {
                            HandleNotificationNep17(notifyEventArgs.ScriptContainer, notifyEventArgs.ScriptHash, stateItems, balanceChangeRecords, ref nep17TransferIndex);
                        }
                        catch (Exception e)
                        {
                            Log(e.ToString(), LogLevel.Error);
                            throw;
                        }
                    }
                }
            }

            //update nep17 balance
            foreach (var balanceChangeRecord in balanceChangeRecords)
            {
                try
                {
                    SaveNep17Balance(balanceChangeRecord, snapshot);
                }
                catch (Exception e)
                {
                    Log(e.ToString(), LogLevel.Error);
                    throw;
                }
            }
        }


        private void HandleNotificationNep17(IVerifiable scriptContainer, UInt160 asset, Array stateItems, HashSet<BalanceChangeRecord> balanceChangeRecords, ref uint transferIndex)
        {
            if (stateItems.Count != 3) return;
            var transferRecord = GetTransferRecord(asset, stateItems);
            if (transferRecord == null) return;
            if (transferRecord.from != UInt160.Zero)
            {
                balanceChangeRecords.Add(new BalanceChangeRecord(transferRecord.from, asset));
            }
            if (transferRecord.to != UInt160.Zero)
            {
                balanceChangeRecords.Add(new BalanceChangeRecord(transferRecord.to, asset));
            }
            if (scriptContainer is Transaction transaction)
            {
                RecordTransferHistoryNep17(asset, transferRecord.from, transferRecord.to, transferRecord.amount, transaction.Hash, ref transferIndex);
            }
        }


        private void SaveNep17Balance(BalanceChangeRecord balanceChanged, DataCache snapshot)
        {
            var key = new Nep17BalanceKey(balanceChanged.User, balanceChanged.Asset);
            using ScriptBuilder sb = new();
            sb.EmitDynamicCall(balanceChanged.Asset, "balanceOf", balanceChanged.User);
            using ApplicationEngine engine = ApplicationEngine.Run(sb.ToArray(), snapshot, settings: _neoSystem.Settings, gas: 1700_0000);

            if (engine.State.HasFlag(VMState.FAULT) || engine.ResultStack.Count == 0)
            {
                Log($"Fault:{balanceChanged.User} get {balanceChanged.Asset} balance fault", LogLevel.Warning);
                return;
            }

            var balanceItem = engine.ResultStack.Pop();
            if (balanceItem is not Integer)
            {
                Log($"Fault:{balanceChanged.User} get {balanceChanged.Asset} balance not number", LogLevel.Warning);
                return;
            }

            var balance = balanceItem.GetInteger();

            if (balance.IsZero)
            {
                Delete(Nep17BalancePrefix, key);
                return;
            }

            Put(Nep17BalancePrefix, key, new TokenBalance { Balance = balance, LastUpdatedBlock = _currentHeight });
        }


        [RpcMethod]
        public JToken GetNep17Transfers(JArray _params)
        {
            Result.True_Or(_params.Count >= 1 || _params.Count <= 3, RpcError.InvalidParams.WithData("Invalid params, need an address, a startTime(optional), a endTime(optional)."));
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
            AddNep17Transfers(Nep17TransferSentPrefix, userScriptHash, startTime, endTime, transfersSent);
            AddNep17Transfers(Nep17TransferReceivedPrefix, userScriptHash, startTime, endTime, transfersReceived);
            return json;
        }

        [RpcMethod]
        public JToken GetNep17Balances(JArray _params)
        {
            Result.True_Or(_params.Count == 1, RpcError.InvalidParams.WithData("Invalid params, need an address."));
            UInt160 userScriptHash = GetScriptHashFromParam(_params[0].AsString());

            JObject json = new();
            JArray balances = new();
            json["address"] = userScriptHash.ToAddress(_neoSystem.Settings.AddressVersion);
            json["balance"] = balances;

            int count = 0;
            byte[] prefix = Key(Nep17BalancePrefix, userScriptHash);
            foreach (var (key, value) in _db.FindPrefix<Nep17BalanceKey, TokenBalance>(prefix))
            {
                if (NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, key.AssetScriptHash) is null)
                    continue;

                try
                {
                    using var script = new ScriptBuilder();
                    script.EmitDynamicCall(key.AssetScriptHash, "decimals");
                    script.EmitDynamicCall(key.AssetScriptHash, "symbol");

                    var engine = ApplicationEngine.Run(script.ToArray(), _neoSystem.StoreView, settings: _neoSystem.Settings);
                    var symbol = engine.ResultStack.Pop().GetString();
                    var decimals = engine.ResultStack.Pop().GetInteger();
                    var name = NativeContract.ContractManagement.GetContract(_neoSystem.StoreView, key.AssetScriptHash).Manifest.Name;

                    balances.Add(new JObject
                    {
                        ["assethash"] = key.AssetScriptHash.ToString(),
                        ["name"] = name,
                        ["symbol"] = symbol,
                        ["decimals"] = decimals.ToString(),
                        ["amount"] = value.Balance.ToString(),
                        ["lastupdatedblock"] = value.LastUpdatedBlock
                    });
                    count++;
                    if (count >= _maxResults)
                    {
                        break;
                    }
                }
                catch { }
            }
            return json;
        }

        private void AddNep17Transfers(byte dbPrefix, UInt160 userScriptHash, ulong startTime, ulong endTime, JArray parentJArray)
        {
            var transferPairs = QueryTransfers<Nep17TransferKey, TokenTransfer>(dbPrefix, userScriptHash, startTime, endTime).Take((int)_maxResults).ToList();
            foreach (var (key, value) in transferPairs.OrderByDescending(l => l.key.TimestampMS))
            {
                parentJArray.Add(ToJson(key, value));
            }
        }


        private void RecordTransferHistoryNep17(UInt160 scriptHash, UInt160 from, UInt160 to, BigInteger amount, UInt256 txHash, ref uint transferIndex)
        {
            if (!_shouldTrackHistory) return;
            if (from != UInt160.Zero)
            {
                Put(Nep17TransferSentPrefix,
                    new Nep17TransferKey(from, _currentBlock.Header.Timestamp, scriptHash, transferIndex),
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
                Put(Nep17TransferReceivedPrefix,
                    new Nep17TransferKey(to, _currentBlock.Header.Timestamp, scriptHash, transferIndex),
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
    }
}
