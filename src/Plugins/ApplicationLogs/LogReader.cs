// Copyright (C) 2015-2024 The Neo Project.
//
// LogReader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.IEventHandlers;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.ApplicationLogs.Store;
using Neo.Plugins.ApplicationLogs.Store.Models;
using Neo.Plugins.RpcServer;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Numerics;
using static System.IO.Path;

namespace Neo.Plugins.ApplicationLogs
{
    public class LogReader : Plugin, ICommittingHandler, ICommittedHandler, ILogHandler
    {
        #region Globals

        internal NeoStore _neostore;
        private NeoSystem _neosystem;
        private readonly List<LogEventArgs> _logEvents;

        #endregion

        public override string Name => "ApplicationLogs";
        public override string Description => "Synchronizes smart contract VM executions and notifications (NotifyLog) on blockchain.";
        protected override UnhandledExceptionPolicy ExceptionPolicy => Settings.Default.ExceptionPolicy;

        #region Ctor

        public LogReader()
        {
            _logEvents = new();
            Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed += ((ICommittedHandler)this).Blockchain_Committed_Handler;
        }

        #endregion

        #region Override Methods

        public override string ConfigFile => Combine(RootPath, "ApplicationLogs.json");

        public override void Dispose()
        {
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed -= ((ICommittedHandler)this).Blockchain_Committed_Handler;
            if (Settings.Default.Debug)
                ApplicationEngine.Log -= ((ILogHandler)this).ApplicationEngine_Log_Handler;
            GC.SuppressFinalize(this);
        }

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Network != Settings.Default.Network)
                return;
            string path = string.Format(Settings.Default.Path, Settings.Default.Network.ToString("X8"));
            var store = system.LoadStore(GetFullPath(path));
            _neostore = new NeoStore(store);
            _neosystem = system;
            RpcServerPlugin.RegisterMethods(this, Settings.Default.Network);

            if (Settings.Default.Debug)
                ApplicationEngine.Log += ((ILogHandler)this).ApplicationEngine_Log_Handler;
        }

        #endregion

        #region JSON RPC Methods

        [RpcMethod]
        public JToken GetApplicationLog(JArray _params)
        {
            if (_params == null || _params.Count == 0)
                throw new RpcException(RpcError.InvalidParams);
            if (UInt256.TryParse(_params[0].AsString(), out var hash))
            {
                var raw = BlockToJObject(hash);
                if (raw == null)
                    raw = TransactionToJObject(hash);
                if (raw == null)
                    throw new RpcException(RpcError.InvalidParams.WithData("Unknown transaction/blockhash"));

                if (_params.Count >= 2 && Enum.TryParse(_params[1].AsString(), true, out TriggerType triggerType))
                {
                    var executions = raw["executions"] as JArray;
                    for (int i = 0; i < executions.Count;)
                    {
                        if (executions[i]["trigger"].AsString().Equals(triggerType.ToString(), StringComparison.OrdinalIgnoreCase) == false)
                            executions.RemoveAt(i);
                        else
                            i++;
                    }
                }

                return raw ?? JToken.Null;
            }
            else
                throw new RpcException(RpcError.InvalidParams);
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("log block", Category = "ApplicationLog Commands")]
        internal void OnGetBlockCommand(string blockHashOrIndex, string eventName = null)
        {
            UInt256 blockhash;
            if (uint.TryParse(blockHashOrIndex, out var blockIndex))
            {
                blockhash = NativeContract.Ledger.GetBlockHash(_neosystem.StoreView, blockIndex);
            }
            else if (UInt256.TryParse(blockHashOrIndex, out blockhash) == false)
            {
                ConsoleHelper.Error("Invalid block hash or index.");
                return;
            }

            var blockOnPersist = string.IsNullOrEmpty(eventName) ?
                _neostore.GetBlockLog(blockhash, TriggerType.OnPersist) :
                _neostore.GetBlockLog(blockhash, TriggerType.OnPersist, eventName);
            var blockPostPersist = string.IsNullOrEmpty(eventName) ?
                _neostore.GetBlockLog(blockhash, TriggerType.PostPersist) :
                _neostore.GetBlockLog(blockhash, TriggerType.PostPersist, eventName);

            if (blockOnPersist == null)
                ConsoleHelper.Error($"No logs.");
            else
            {
                PrintExecutionToConsole(blockOnPersist);
                ConsoleHelper.Info("--------------------------------");
                PrintExecutionToConsole(blockPostPersist);
            }
        }

        [ConsoleCommand("log tx", Category = "ApplicationLog Commands")]
        internal void OnGetTransactionCommand(UInt256 txhash, string eventName = null)
        {
            var txApplication = string.IsNullOrEmpty(eventName) ?
                _neostore.GetTransactionLog(txhash) :
                _neostore.GetTransactionLog(txhash, eventName);

            if (txApplication == null)
                ConsoleHelper.Error($"No logs.");
            else
                PrintExecutionToConsole(txApplication);
        }

        [ConsoleCommand("log contract", Category = "ApplicationLog Commands")]
        internal void OnGetContractCommand(UInt160 scripthash, uint page = 1, uint pageSize = 1, string eventName = null)
        {
            if (page == 0)
            {
                ConsoleHelper.Error("Page is invalid. Pick a number 1 and above.");
                return;
            }

            if (pageSize == 0)
            {
                ConsoleHelper.Error("PageSize is invalid. Pick a number between 1 and 10.");
                return;
            }

            var txContract = string.IsNullOrEmpty(eventName) ?
                _neostore.GetContractLog(scripthash, TriggerType.Application, page, pageSize) :
                _neostore.GetContractLog(scripthash, TriggerType.Application, eventName, page, pageSize);

            if (txContract.Count == 0)
                ConsoleHelper.Error($"No logs.");
            else
                PrintEventModelToConsole(txContract);
        }


        #endregion

        #region Blockchain Events

        void ICommittingHandler.Blockchain_Committing_Handler(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network != Settings.Default.Network)
                return;

            if (_neostore is null)
                return;
            _neostore.StartBlockLogBatch();
            _neostore.PutBlockLog(block, applicationExecutedList);
            if (Settings.Default.Debug)
            {
                foreach (var appEng in applicationExecutedList.Where(w => w.Transaction != null))
                {
                    var logs = _logEvents.Where(w => w.ScriptContainer.Hash == appEng.Transaction.Hash).ToList();
                    if (logs.Any())
                        _neostore.PutTransactionEngineLogState(appEng.Transaction.Hash, logs);
                }
                _logEvents.Clear();
            }
        }

        void ICommittedHandler.Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (system.Settings.Network != Settings.Default.Network)
                return;
            if (_neostore is null)
                return;
            _neostore.CommitBlockLog();
        }

        void ILogHandler.ApplicationEngine_Log_Handler(object sender, LogEventArgs e)
        {
            if (Settings.Default.Debug == false)
                return;

            if (_neosystem.Settings.Network != Settings.Default.Network)
                return;

            if (e.ScriptContainer == null)
                return;

            _logEvents.Add(e);
        }

        #endregion

        #region Private Methods

        private void PrintExecutionToConsole(BlockchainExecutionModel model)
        {
            ConsoleHelper.Info("Trigger: ", $"{model.Trigger}");
            ConsoleHelper.Info("VM State: ", $"{model.VmState}");
            if (string.IsNullOrEmpty(model.Exception) == false)
                ConsoleHelper.Error($"Exception: {model.Exception}");
            else
                ConsoleHelper.Info("Exception: ", "null");
            ConsoleHelper.Info("Gas Consumed: ", $"{new BigDecimal((BigInteger)model.GasConsumed, NativeContract.GAS.Decimals)}");
            if (model.Stack.Length == 0)
                ConsoleHelper.Info("Stack: ", "[]");
            else
            {
                ConsoleHelper.Info("Stack: ");
                for (int i = 0; i < model.Stack.Length; i++)
                    ConsoleHelper.Info($"  {i}: ", $"{model.Stack[i].ToJson()}");
            }
            if (model.Notifications.Length == 0)
                ConsoleHelper.Info("Notifications: ", "[]");
            else
            {
                ConsoleHelper.Info("Notifications:");
                foreach (var notifyItem in model.Notifications)
                {
                    ConsoleHelper.Info();
                    ConsoleHelper.Info("  ScriptHash: ", $"{notifyItem.ScriptHash}");
                    ConsoleHelper.Info("  Event Name: ", $"{notifyItem.EventName}");
                    ConsoleHelper.Info("  State Parameters:");
                    var ncount = (uint)notifyItem.State.Length;
                    for (var i = 0; i < ncount; i++)
                        ConsoleHelper.Info($"    {GetMethodParameterName(notifyItem.ScriptHash, notifyItem.EventName, ncount, i)}: ", $"{notifyItem.State[i].ToJson()}");
                }
            }
            if (Settings.Default.Debug)
            {
                if (model.Logs.Length == 0)
                    ConsoleHelper.Info("Logs: ", "[]");
                else
                {
                    ConsoleHelper.Info("Logs:");
                    foreach (var logItem in model.Logs)
                    {
                        ConsoleHelper.Info();
                        ConsoleHelper.Info("  ScriptHash: ", $"{logItem.ScriptHash}");
                        ConsoleHelper.Info("  Message: ", $"{logItem.Message}");
                    }
                }
            }
        }

        private void PrintEventModelToConsole(IReadOnlyCollection<(BlockchainEventModel NotifyLog, UInt256 TxHash)> models)
        {
            foreach (var (notifyItem, txhash) in models)
            {
                ConsoleHelper.Info("Transaction Hash: ", $"{txhash}");
                ConsoleHelper.Info();
                ConsoleHelper.Info("  Event Name:  ", $"{notifyItem.EventName}");
                ConsoleHelper.Info("  State Parameters:");
                var ncount = (uint)notifyItem.State.Length;
                for (var i = 0; i < ncount; i++)
                    ConsoleHelper.Info($"    {GetMethodParameterName(notifyItem.ScriptHash, notifyItem.EventName, ncount, i)}: ", $"{notifyItem.State[i].ToJson()}");
                ConsoleHelper.Info("--------------------------------");
            }
        }

        private string GetMethodParameterName(UInt160 scriptHash, string methodName, uint ncount, int parameterIndex)
        {
            var contract = NativeContract.ContractManagement.GetContract(_neosystem.StoreView, scriptHash);
            if (contract == null)
                return $"{parameterIndex}";
            var contractEvent = contract.Manifest.Abi.Events.SingleOrDefault(s => s.Name == methodName && (uint)s.Parameters.Length == ncount);
            if (contractEvent == null)
                return $"{parameterIndex}";
            return contractEvent.Parameters[parameterIndex].Name;
        }

        private JObject EventModelToJObject(BlockchainEventModel model)
        {
            var root = new JObject();
            root["contract"] = model.ScriptHash.ToString();
            root["eventname"] = model.EventName;
            root["state"] = model.State.Select(s => s.ToJson()).ToArray();
            return root;
        }

        private JObject TransactionToJObject(UInt256 txHash)
        {
            var appLog = _neostore.GetTransactionLog(txHash);
            if (appLog == null)
                return null;

            var raw = new JObject();
            raw["txid"] = txHash.ToString();

            var trigger = new JObject();
            trigger["trigger"] = appLog.Trigger;
            trigger["vmstate"] = appLog.VmState;
            trigger["exception"] = string.IsNullOrEmpty(appLog.Exception) ? null : appLog.Exception;
            trigger["gasconsumed"] = appLog.GasConsumed.ToString();

            try
            {
                trigger["stack"] = appLog.Stack.Select(s => s.ToJson(Settings.Default.MaxStackSize)).ToArray();
            }
            catch (Exception ex)
            {
                trigger["exception"] = ex.Message;
            }

            trigger["notifications"] = appLog.Notifications.Select(s =>
            {
                var notification = new JObject();
                notification["contract"] = s.ScriptHash.ToString();
                notification["eventname"] = s.EventName;

                try
                {
                    var state = new JObject();
                    state["type"] = "Array";
                    state["value"] = s.State.Select(ss => ss.ToJson()).ToArray();

                    notification["state"] = state;
                }
                catch (InvalidOperationException)
                {
                    notification["state"] = "error: recursive reference";
                }

                return notification;
            }).ToArray();

            if (Settings.Default.Debug)
            {
                trigger["logs"] = appLog.Logs.Select(s =>
                {
                    var log = new JObject();
                    log["contract"] = s.ScriptHash.ToString();
                    log["message"] = s.Message;
                    return log;
                }).ToArray();
            }

            raw["executions"] = new[] { trigger };
            return raw;
        }

        private JObject BlockToJObject(UInt256 blockHash)
        {
            var blockOnPersist = _neostore.GetBlockLog(blockHash, TriggerType.OnPersist);
            var blockPostPersist = _neostore.GetBlockLog(blockHash, TriggerType.PostPersist);

            if (blockOnPersist == null && blockPostPersist == null)
                return null;

            var blockJson = new JObject();
            blockJson["blockhash"] = blockHash.ToString();
            var triggerList = new List<JObject>();

            if (blockOnPersist != null)
                triggerList.Add(BlockItemToJObject(blockOnPersist));
            if (blockPostPersist != null)
                triggerList.Add(BlockItemToJObject(blockPostPersist));

            blockJson["executions"] = triggerList.ToArray();
            return blockJson;
        }

        private JObject BlockItemToJObject(BlockchainExecutionModel blockExecutionModel)
        {
            JObject trigger = new();
            trigger["trigger"] = blockExecutionModel.Trigger;
            trigger["vmstate"] = blockExecutionModel.VmState;
            trigger["gasconsumed"] = blockExecutionModel.GasConsumed.ToString();
            try
            {
                trigger["stack"] = blockExecutionModel.Stack.Select(q => q.ToJson(Settings.Default.MaxStackSize)).ToArray();
            }
            catch (Exception ex)
            {
                trigger["exception"] = ex.Message;
            }
            trigger["notifications"] = blockExecutionModel.Notifications.Select(s =>
            {
                JObject notification = new();
                notification["contract"] = s.ScriptHash.ToString();
                notification["eventname"] = s.EventName;
                try
                {
                    var state = new JObject();
                    state["type"] = "Array";
                    state["value"] = s.State.Select(ss => ss.ToJson()).ToArray();

                    notification["state"] = state;
                }
                catch (InvalidOperationException)
                {
                    notification["state"] = "error: recursive reference";
                }
                return notification;
            }).ToArray();

            if (Settings.Default.Debug)
            {
                trigger["logs"] = blockExecutionModel.Logs.Select(s =>
                {
                    var log = new JObject();
                    log["contract"] = s.ScriptHash.ToString();
                    log["message"] = s.Message;
                    return log;
                }).ToArray();
            }

            return trigger;
        }

        #endregion
    }
}
