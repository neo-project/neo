// Copyright (C) 2015-2024 The Neo Project.
//
// OracleService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Util.Internal;
using Neo.ConsoleService;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IEventHandlers;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.OracleService
{
    public class OracleService : Plugin, ICommittingHandler, IServiceAddedHandler, IWalletChangedHandler
    {
        private const int RefreshIntervalMilliSeconds = 1000 * 60 * 3;

        private static readonly HttpClient httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5),
            MaxResponseContentBufferSize = ushort.MaxValue
        };

        private Wallet wallet;
        private readonly ConcurrentDictionary<ulong, OracleTask> pendingQueue = new ConcurrentDictionary<ulong, OracleTask>();
        private readonly ConcurrentDictionary<ulong, DateTime> finishedCache = new ConcurrentDictionary<ulong, DateTime>();
        private Timer timer;
        public readonly CancellationTokenSource cancelSource = new CancellationTokenSource();
        private OracleStatus status = OracleStatus.Unstarted;
        private IWalletProvider walletProvider;
        private int counter;
        private NeoSystem _system;

        private readonly Dictionary<string, IOracleProtocol> protocols = new Dictionary<string, IOracleProtocol>();

        public override string Description => "Built-in oracle plugin";

        protected override UnhandledExceptionPolicy ExceptionPolicy => Settings.Default.ExceptionPolicy;

        public override string ConfigFile => System.IO.Path.Combine(RootPath, "OracleService.json");

        public OracleService()
        {
            Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
        }

        protected override void Configure()
        {
            Settings.Load(GetConfiguration());
            foreach (var (_, p) in protocols)
                p.Configure();
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (system.Settings.Network != Settings.Default.Network) return;
            _system = system;
            _system.ServiceAdded += ((IServiceAddedHandler)this).NeoSystem_ServiceAdded_Handler;
            RpcServerPlugin.RegisterMethods(this, Settings.Default.Network);
        }


        void IServiceAddedHandler.NeoSystem_ServiceAdded_Handler(object sender, object service)
        {
            if (service is IWalletProvider)
            {
                walletProvider = service as IWalletProvider;
                _system.ServiceAdded -= ((IServiceAddedHandler)this).NeoSystem_ServiceAdded_Handler;
                if (Settings.Default.AutoStart)
                {
                    walletProvider.WalletChanged += ((IWalletChangedHandler)this).IWalletProvider_WalletChanged_Handler;
                }
            }
        }

        void IWalletChangedHandler.IWalletProvider_WalletChanged_Handler(object sender, Wallet wallet)
        {
            walletProvider.WalletChanged -= ((IWalletChangedHandler)this).IWalletProvider_WalletChanged_Handler;
            Start(wallet);
        }

        public override void Dispose()
        {
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            OnStop();
            while (status != OracleStatus.Stopped)
                Thread.Sleep(100);
            foreach (var p in protocols)
                p.Value.Dispose();
        }

        [ConsoleCommand("start oracle", Category = "Oracle", Description = "Start oracle service")]
        private void OnStart()
        {
            Start(walletProvider?.GetWallet());
        }

        public Task Start(Wallet wallet)
        {
            if (status == OracleStatus.Running) return null;

            if (wallet is null)
            {
                ConsoleHelper.Warning("Please open wallet first!");
                return null;
            }

            if (!CheckOracleAvailable(_system.StoreView, out ECPoint[] oracles))
            {
                ConsoleHelper.Warning("The oracle service is unavailable");
                return null;
            }
            if (!CheckOracleAccount(wallet, oracles))
            {
                ConsoleHelper.Warning("There is no oracle account in wallet");
                return null;
            }

            this.wallet = wallet;
            protocols["https"] = new OracleHttpsProtocol();
            protocols["neofs"] = new OracleNeoFSProtocol(wallet, oracles);
            status = OracleStatus.Running;
            timer = new Timer(OnTimer, null, RefreshIntervalMilliSeconds, Timeout.Infinite);
            ConsoleHelper.Info($"Oracle started");
            return ProcessRequestsAsync();
        }

        [ConsoleCommand("stop oracle", Category = "Oracle", Description = "Stop oracle service")]
        private void OnStop()
        {
            cancelSource.Cancel();
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            status = OracleStatus.Stopped;
        }

        [ConsoleCommand("oracle status", Category = "Oracle", Description = "Show oracle status")]
        private void OnShow()
        {
            ConsoleHelper.Info($"Oracle status: ", $"{status}");
        }

        void ICommittingHandler.Blockchain_Committing_Handler(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            if (system.Settings.Network != Settings.Default.Network) return;

            if (Settings.Default.AutoStart && status == OracleStatus.Unstarted)
            {
                OnStart();
            }
            if (status != OracleStatus.Running) return;
            if (!CheckOracleAvailable(snapshot, out ECPoint[] oracles) || !CheckOracleAccount(wallet, oracles))
                OnStop();
        }

        private async void OnTimer(object state)
        {
            try
            {
                List<ulong> outOfDate = new();
                List<Task> tasks = new();
                foreach (var (id, task) in pendingQueue)
                {
                    var span = TimeProvider.Current.UtcNow - task.Timestamp;
                    if (span > Settings.Default.MaxTaskTimeout)
                    {
                        outOfDate.Add(id);
                        continue;
                    }

                    if (span > TimeSpan.FromMilliseconds(RefreshIntervalMilliSeconds))
                    {
                        foreach (var account in wallet.GetAccounts())
                            if (task.BackupSigns.TryGetValue(account.GetKey().PublicKey, out byte[] sign))
                                tasks.Add(SendResponseSignatureAsync(id, sign, account.GetKey()));
                    }
                }

                await Task.WhenAll(tasks);

                foreach (ulong requestId in outOfDate)
                    pendingQueue.TryRemove(requestId, out _);
                foreach (var (key, value) in finishedCache)
                    if (TimeProvider.Current.UtcNow - value > TimeSpan.FromDays(3))
                        finishedCache.TryRemove(key, out _);
            }
            catch (Exception e)
            {
                Log(e, LogLevel.Error);
            }
            finally
            {
                if (!cancelSource.IsCancellationRequested)
                    timer?.Change(RefreshIntervalMilliSeconds, Timeout.Infinite);
            }
        }

        [RpcMethod]
        public JObject SubmitOracleResponse(JArray _params)
        {
            status.Equals(OracleStatus.Running).True_Or(RpcError.OracleDisabled);
            ECPoint oraclePub = ECPoint.DecodePoint(Convert.FromBase64String(_params[0].AsString()), ECCurve.Secp256r1);
            ulong requestId = Result.Ok_Or(() => (ulong)_params[1].AsNumber(), RpcError.InvalidParams.WithData($"Invalid requestId: {_params[1]}"));
            byte[] txSign = Result.Ok_Or(() => Convert.FromBase64String(_params[2].AsString()), RpcError.InvalidParams.WithData($"Invalid txSign: {_params[2]}"));
            byte[] msgSign = Result.Ok_Or(() => Convert.FromBase64String(_params[3].AsString()), RpcError.InvalidParams.WithData($"Invalid msgSign: {_params[3]}"));

            finishedCache.ContainsKey(requestId).False_Or(RpcError.OracleRequestFinished);

            using (var snapshot = _system.GetSnapshotCache())
            {
                uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
                var oracles = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
                oracles.Any(p => p.Equals(oraclePub)).True_Or(RpcErrorFactory.OracleNotDesignatedNode(oraclePub));
                NativeContract.Oracle.GetRequest(snapshot, requestId).NotNull_Or(RpcError.OracleRequestNotFound);
                byte[] data = [.. oraclePub.ToArray(), .. BitConverter.GetBytes(requestId), .. txSign];
                Crypto.VerifySignature(data, msgSign, oraclePub).True_Or(RpcErrorFactory.InvalidSignature($"Invalid oracle response transaction signature from '{oraclePub}'."));
                AddResponseTxSign(snapshot, requestId, oraclePub, txSign);
            }
            return new JObject();
        }

        private static async Task SendContentAsync(Uri url, string content)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Log($"Failed to send the response signature to {url}, as {e.Message}", LogLevel.Warning);
            }
        }

        private async Task SendResponseSignatureAsync(ulong requestId, byte[] txSign, KeyPair keyPair)
        {
            byte[] message = [.. keyPair.PublicKey.ToArray(), .. BitConverter.GetBytes(requestId), .. txSign];
            var sign = Crypto.Sign(message, keyPair.PrivateKey);
            var param = "\"" + Convert.ToBase64String(keyPair.PublicKey.ToArray()) + "\", " + requestId + ", \"" + Convert.ToBase64String(txSign) + "\",\"" + Convert.ToBase64String(sign) + "\"";
            var content = "{\"id\":" + Interlocked.Increment(ref counter) + ",\"jsonrpc\":\"2.0\",\"method\":\"submitoracleresponse\",\"params\":[" + param + "]}";

            var tasks = Settings.Default.Nodes.Select(p => SendContentAsync(p, content));
            await Task.WhenAll(tasks);
        }

        private async Task ProcessRequestAsync(DataCache snapshot, OracleRequest req)
        {
            Log($"[{req.OriginalTxid}] Process oracle request start:<{req.Url}>");

            uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;

            (OracleResponseCode code, string data) = await ProcessUrlAsync(req.Url);

            Log($"[{req.OriginalTxid}] Process oracle request end:<{req.Url}>, responseCode:{code}, response:{data}");

            var oracleNodes = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
            foreach (var (requestId, request) in NativeContract.Oracle.GetRequestsByUrl(snapshot, req.Url))
            {
                var result = Array.Empty<byte>();
                if (code == OracleResponseCode.Success)
                {
                    try
                    {
                        result = Filter(data, request.Filter);
                    }
                    catch (Exception ex)
                    {
                        code = OracleResponseCode.Error;
                        Log($"[{req.OriginalTxid}] Filter '{request.Filter}' error:{ex.Message}");
                    }
                }
                var response = new OracleResponse() { Id = requestId, Code = code, Result = result };
                var responseTx = CreateResponseTx(snapshot, request, response, oracleNodes, _system.Settings);
                var backupTx = CreateResponseTx(snapshot, request, new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = requestId, Result = Array.Empty<byte>() }, oracleNodes, _system.Settings, true);

                Log($"[{req.OriginalTxid}]-({requestId}) Built response tx[[{responseTx.Hash}]], responseCode:{code}, result:{result.ToHexString()}, validUntilBlock:{responseTx.ValidUntilBlock}, backupTx:{backupTx.Hash}-{backupTx.ValidUntilBlock}");

                List<Task> tasks = new List<Task>();
                ECPoint[] oraclePublicKeys = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
                foreach (var account in wallet.GetAccounts())
                {
                    var oraclePub = account.GetKey()?.PublicKey;
                    if (!account.HasKey || account.Lock || !oraclePublicKeys.Contains(oraclePub)) continue;

                    var txSign = responseTx.Sign(account.GetKey(), _system.Settings.Network);
                    var backTxSign = backupTx.Sign(account.GetKey(), _system.Settings.Network);

                    AddResponseTxSign(snapshot, requestId, oraclePub, txSign, responseTx, backupTx, backTxSign);
                    tasks.Add(SendResponseSignatureAsync(requestId, txSign, account.GetKey()));

                    Log($"[{request.OriginalTxid}]-[[{responseTx.Hash}]] Send oracle sign data, Oracle node: {oraclePub}, Sign: {txSign.ToHexString()}");
                }
                await Task.WhenAll(tasks);
            }
        }

        private async Task ProcessRequestsAsync()
        {
            while (!cancelSource.IsCancellationRequested)
            {
                using (var snapshot = _system.GetSnapshotCache())
                {
                    SyncPendingQueue(snapshot);
                    foreach (var (id, request) in NativeContract.Oracle.GetRequests(snapshot))
                    {
                        if (cancelSource.IsCancellationRequested) break;
                        if (!finishedCache.ContainsKey(id) && (!pendingQueue.TryGetValue(id, out OracleTask task) || task.Tx is null))
                            await ProcessRequestAsync(snapshot, request);
                    }
                }
                if (cancelSource.IsCancellationRequested) break;
                await Task.Delay(500);
            }

            status = OracleStatus.Stopped;
        }


        private void SyncPendingQueue(DataCache snapshot)
        {
            var offChainRequests = NativeContract.Oracle.GetRequests(snapshot).ToDictionary(r => r.Item1, r => r.Item2);
            var onChainRequests = pendingQueue.Keys.Except(offChainRequests.Keys);
            foreach (var onChainRequest in onChainRequests)
            {
                pendingQueue.TryRemove(onChainRequest, out _);
            }
        }

        private async Task<(OracleResponseCode, string)> ProcessUrlAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return (OracleResponseCode.Error, $"Invalid url:<{url}>");
            if (!protocols.TryGetValue(uri.Scheme, out IOracleProtocol protocol))
                return (OracleResponseCode.ProtocolNotSupported, $"Invalid Protocol:<{url}>");

            using CancellationTokenSource ctsTimeout = new(Settings.Default.MaxOracleTimeout);
            using CancellationTokenSource ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, ctsTimeout.Token);

            try
            {
                return await protocol.ProcessAsync(uri, ctsLinked.Token);
            }
            catch (Exception ex)
            {
                return (OracleResponseCode.Error, $"Request <{url}> Error:{ex.Message}");
            }
        }

        public static Transaction CreateResponseTx(DataCache snapshot, OracleRequest request, OracleResponse response, ECPoint[] oracleNodes, ProtocolSettings settings, bool useCurrentHeight = false)
        {
            var requestTx = NativeContract.Ledger.GetTransactionState(snapshot, request.OriginalTxid);
            var n = oracleNodes.Length;
            var m = n - (n - 1) / 3;
            var oracleSignContract = Contract.CreateMultiSigContract(m, oracleNodes);
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            var validUntilBlock = requestTx.BlockIndex + settings.MaxValidUntilBlockIncrement;
            while (useCurrentHeight && validUntilBlock <= height)
            {
                validUntilBlock += settings.MaxValidUntilBlockIncrement;
            }
            var tx = new Transaction()
            {
                Version = 0,
                Nonce = unchecked((uint)response.Id),
                ValidUntilBlock = validUntilBlock,
                Signers = new[]
                {
                    new Signer
                    {
                        Account = NativeContract.Oracle.Hash,
                        Scopes = WitnessScope.None
                    },
                    new Signer
                    {
                        Account = oracleSignContract.ScriptHash,
                        Scopes = WitnessScope.None
                    }
                },
                Attributes = new[] { response },
                Script = OracleResponse.FixedScript,
                Witnesses = new Witness[2]
            };
            Dictionary<UInt160, Witness> witnessDict = new Dictionary<UInt160, Witness>
            {
                [oracleSignContract.ScriptHash] = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = oracleSignContract.Script,
                },
                [NativeContract.Oracle.Hash] = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>(),
                }
            };

            UInt160[] hashes = tx.GetScriptHashesForVerifying(snapshot);
            tx.Witnesses[0] = witnessDict[hashes[0]];
            tx.Witnesses[1] = witnessDict[hashes[1]];

            // Calculate network fee

            var oracleContract = NativeContract.ContractManagement.GetContract(snapshot, NativeContract.Oracle.Hash);
            var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot.CloneCache(), settings: settings);
            ContractMethodDescriptor md = oracleContract.Manifest.Abi.GetMethod(ContractBasicMethod.Verify, ContractBasicMethod.VerifyPCount);
            engine.LoadContract(oracleContract, md, CallFlags.None);
            if (engine.Execute() != VMState.HALT) return null;
            tx.NetworkFee += engine.FeeConsumed;

            var executionFactor = NativeContract.Policy.GetExecFeeFactor(snapshot);
            var networkFee = executionFactor * SmartContract.Helper.MultiSignatureContractCost(m, n);
            tx.NetworkFee += networkFee;

            // Base size for transaction: includes const_header + signers + script + hashes + witnesses, except attributes

            int size_inv = 66 * m;
            int size = Transaction.HeaderSize + tx.Signers.GetVarSize() + tx.Script.GetVarSize()
                + IO.Helper.GetVarSize(hashes.Length) + witnessDict[NativeContract.Oracle.Hash].Size
                + IO.Helper.GetVarSize(size_inv) + size_inv + oracleSignContract.Script.GetVarSize();

            var feePerByte = NativeContract.Policy.GetFeePerByte(snapshot);
            if (response.Result.Length > OracleResponse.MaxResultSize)
            {
                response.Code = OracleResponseCode.ResponseTooLarge;
                response.Result = Array.Empty<byte>();
            }
            else if (tx.NetworkFee + (size + tx.Attributes.GetVarSize()) * feePerByte > request.GasForResponse)
            {
                response.Code = OracleResponseCode.InsufficientFunds;
                response.Result = Array.Empty<byte>();
            }
            size += tx.Attributes.GetVarSize();
            tx.NetworkFee += size * feePerByte;

            // Calcualte system fee

            tx.SystemFee = request.GasForResponse - tx.NetworkFee;

            return tx;
        }

        private void AddResponseTxSign(DataCache snapshot, ulong requestId, ECPoint oraclePub, byte[] sign, Transaction responseTx = null, Transaction backupTx = null, byte[] backupSign = null)
        {
            var task = pendingQueue.GetOrAdd(requestId, _ => new OracleTask
            {
                Id = requestId,
                Request = NativeContract.Oracle.GetRequest(snapshot, requestId),
                Signs = new ConcurrentDictionary<ECPoint, byte[]>(),
                BackupSigns = new ConcurrentDictionary<ECPoint, byte[]>()
            });

            if (responseTx != null)
            {
                task.Tx = responseTx;
                var data = task.Tx.GetSignData(_system.Settings.Network);
                task.Signs.Where(p => !Crypto.VerifySignature(data, p.Value, p.Key)).ForEach(p => task.Signs.Remove(p.Key, out _));
            }
            if (backupTx != null)
            {
                task.BackupTx = backupTx;
                var data = task.BackupTx.GetSignData(_system.Settings.Network);
                task.BackupSigns.Where(p => !Crypto.VerifySignature(data, p.Value, p.Key)).ForEach(p => task.BackupSigns.Remove(p.Key, out _));
                task.BackupSigns.TryAdd(oraclePub, backupSign);
            }
            if (task.Tx == null)
            {
                task.Signs.TryAdd(oraclePub, sign);
                task.BackupSigns.TryAdd(oraclePub, sign);
                return;
            }

            if (Crypto.VerifySignature(task.Tx.GetSignData(_system.Settings.Network), sign, oraclePub))
                task.Signs.TryAdd(oraclePub, sign);
            else if (Crypto.VerifySignature(task.BackupTx.GetSignData(_system.Settings.Network), sign, oraclePub))
                task.BackupSigns.TryAdd(oraclePub, sign);
            else
                throw new RpcException(RpcErrorFactory.InvalidSignature($"Invalid oracle response transaction signature from '{oraclePub}'."));

            if (CheckTxSign(snapshot, task.Tx, task.Signs) || CheckTxSign(snapshot, task.BackupTx, task.BackupSigns))
            {
                finishedCache.TryAdd(requestId, new DateTime());
                pendingQueue.TryRemove(requestId, out _);
            }
        }

        public static byte[] Filter(string input, string filterArgs)
        {
            if (string.IsNullOrEmpty(filterArgs))
                return Utility.StrictUTF8.GetBytes(input);

            JToken beforeObject = JToken.Parse(input);
            JArray afterObjects = beforeObject.JsonPath(filterArgs);
            return afterObjects.ToByteArray(false);
        }

        private bool CheckTxSign(DataCache snapshot, Transaction tx, ConcurrentDictionary<ECPoint, byte[]> OracleSigns)
        {
            uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
            if (tx.ValidUntilBlock <= height)
            {
                return false;
            }
            ECPoint[] oraclesNodes = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
            int neededThreshold = oraclesNodes.Length - (oraclesNodes.Length - 1) / 3;
            if (OracleSigns.Count >= neededThreshold)
            {
                var contract = Contract.CreateMultiSigContract(neededThreshold, oraclesNodes);
                ScriptBuilder sb = new ScriptBuilder();
                foreach (var (_, sign) in OracleSigns.OrderBy(p => p.Key))
                {
                    sb.EmitPush(sign);
                    if (--neededThreshold == 0) break;
                }
                var idx = tx.GetScriptHashesForVerifying(snapshot)[0] == contract.ScriptHash ? 0 : 1;
                tx.Witnesses[idx].InvocationScript = sb.ToArray();

                Log($"Send response tx: responseTx={tx.Hash}");

                _system.Blockchain.Tell(tx);
                return true;
            }
            return false;
        }

        private static bool CheckOracleAvailable(DataCache snapshot, out ECPoint[] oracles)
        {
            uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
            oracles = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
            return oracles.Length > 0;
        }

        private static bool CheckOracleAccount(Wallet wallet, ECPoint[] oracles)
        {
            if (wallet is null) return false;
            return oracles
                .Select(p => wallet.GetAccount(p))
                .Any(p => p is not null && p.HasKey && !p.Lock);
        }

        private static void Log(string message, LogLevel level = LogLevel.Info)
        {
            Utility.Log(nameof(OracleService), level, message);
        }

        class OracleTask
        {
            public ulong Id;
            public OracleRequest Request;
            public Transaction Tx;
            public Transaction BackupTx;
            public ConcurrentDictionary<ECPoint, byte[]> Signs;
            public ConcurrentDictionary<ECPoint, byte[]> BackupSigns;
            public readonly DateTime Timestamp = TimeProvider.Current.UtcNow;
        }

        enum OracleStatus
        {
            Unstarted,
            Running,
            Stopped,
        }
    }
}
