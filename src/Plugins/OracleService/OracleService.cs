// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.RpcServer;
using Neo.Sign;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        internal readonly CancellationTokenSource cancelSource = new CancellationTokenSource();
        private OracleStatus status = OracleStatus.Unstarted;
        private IWalletProvider walletProvider;
        private int counter;
        private NeoSystem _system;

        private readonly Dictionary<string, IOracleProtocol> protocols = new Dictionary<string, IOracleProtocol>();

        // Serilog logger instance
        private static readonly ILogger _log = Log.ForContext<OracleService>();

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
            if (status == OracleStatus.Running)
            {
                _log.Information("Oracle service already running.");
                return Task.CompletedTask;
            }

            if (wallet is null)
            {
                _log.Warning("Oracle start failed: Wallet not open.");
                ConsoleHelper.Warning("Please open wallet first!");
                return Task.CompletedTask;
            }

            if (!CheckOracleAvailable(_system.StoreView, out ECPoint[] oracles))
            {
                _log.Warning("Oracle start failed: Service is unavailable (no designated oracle nodes?).");
                ConsoleHelper.Warning("The oracle service is unavailable");
                return Task.CompletedTask;
            }
            if (!CheckOracleAccount(wallet, oracles))
            {
                _log.Warning("Oracle start failed: Wallet does not contain any designated oracle account.");
                ConsoleHelper.Warning("There is no oracle account in wallet");
                return Task.CompletedTask;
            }

            this.wallet = wallet;
            _log.Information("Initializing Oracle protocols...");
            protocols["https"] = new OracleHttpsProtocol();
            protocols["neofs"] = new OracleNeoFSProtocol(wallet, oracles);
            status = OracleStatus.Running;
            timer = new Timer(OnTimer, null, RefreshIntervalMilliSeconds, Timeout.Infinite);
            _log.Information("Oracle service started.");
            ConsoleHelper.Info($"Oracle started");
            return ProcessRequestsAsync();
        }

        [ConsoleCommand("stop oracle", Category = "Oracle", Description = "Stop oracle service")]
        private void OnStop()
        {
            _log.Information("Stopping Oracle service...");
            if (!cancelSource.IsCancellationRequested)
            {
                cancelSource.Cancel();
                _log.Information("Cancellation requested via CancelSource.");
            }
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
                _log.Debug("Oracle timer disposed.");
            }
            _log.Information("Oracle service stop request processed.");
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
                _log.Information("Auto-starting Oracle service due to block commit.");
                OnStart();
            }
            if (status != OracleStatus.Running) return;

            // Check if still eligible to be an oracle
            if (!CheckOracleAvailable(snapshot, out ECPoint[] oracles) || !CheckOracleAccount(wallet, oracles))
            {
                _log.Warning("Oracle service stopping: No longer an available/designated oracle node.");
                OnStop();
            }
        }

        private async void OnTimer(object state)
        {
            _log.Verbose("Oracle timer tick: Checking pending/finished tasks.");
            try
            {
                List<ulong> outOfDate = new();
                List<Task> tasks = new();
                int pendingCount = pendingQueue.Count;
                int finishedCount = finishedCache.Count;

                foreach (var (id, task) in pendingQueue)
                {
                    var span = TimeProvider.Current.UtcNow - task.Timestamp;
                    if (span > Settings.Default.MaxTaskTimeout)
                    {
                        _log.Warning("Oracle request {RequestId} timed out after {ElapsedSeconds}s, removing from pending queue.", id, span.TotalSeconds);
                        outOfDate.Add(id);
                        continue;
                    }

                    // Resend signatures periodically
                    if (span > TimeSpan.FromMilliseconds(RefreshIntervalMilliSeconds))
                    {
                        _log.Debug("Resending signatures for oracle request {RequestId}", id);
                        foreach (var account in wallet.GetAccounts())
                        {
                            if (task.BackupSigns.TryGetValue(account.GetKey().PublicKey, out byte[] sign))
                                tasks.Add(SendResponseSignatureAsync(id, sign, account.GetKey()));
                        }
                    }
                }

                await Task.WhenAll(tasks);

                foreach (ulong requestId in outOfDate)
                    pendingQueue.TryRemove(requestId, out _);

                // Clean finished cache
                int removedFinished = 0;
                foreach (var (key, value) in finishedCache)
                {
                    if (TimeProvider.Current.UtcNow - value > TimeSpan.FromDays(3))
                    {
                        if (finishedCache.TryRemove(key, out _))
                            removedFinished++;
                    }
                }
                if (removedFinished > 0) _log.Debug("Removed {RemovedCount} entries older than 3 days from finished cache.", removedFinished);
                _log.Debug("Oracle timer finished. PendingTasks={PendingCount}, FinishedTasks={FinishedCount}", pendingQueue.Count, finishedCache.Count);
            }
            catch (Exception e)
            {
                // Replace Log(e, LogLevel.Error)
                _log.Error(e, "Error occurred during Oracle timer execution.");
            }
            finally
            {
                if (!cancelSource.IsCancellationRequested)
                {
                    _log.Verbose("Rescheduling Oracle timer for {IntervalMs} ms", RefreshIntervalMilliSeconds);
                    timer?.Change(RefreshIntervalMilliSeconds, Timeout.Infinite);
                }
                else
                {
                    _log.Information("Oracle timer not rescheduled as service is stopping.");
                }
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
            _log.Verbose("Sending oracle response signature to {Url}", url);
            try
            {
                using HttpResponseMessage response = await httpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    _log.Warning("Failed to send oracle response signature to {Url}: StatusCode={StatusCode}", url, response.StatusCode);
                else
                    _log.Verbose("Successfully sent oracle response signature to {Url}", url);
                // EnsureSuccessStatusCode(); // Don't throw, just log warning
            }
            catch (Exception e)
            {
                // Replace Log(..., LogLevel.Warning)
                _log.Warning(e, "Exception while sending oracle response signature to {Url}", url);
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
            // Replace Log(...) calls
            _log.Information("[TxId={TxId}] Processing oracle request: Url=<{Url}>, Filter=<{Filter}>", req.OriginalTxid, req.Url, req.Filter);
            var sw = Stopwatch.StartNew();

            uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
            (OracleResponseCode code, string data) = await ProcessUrlAsync(req.Url);
            sw.Stop();

            _log.Information("[TxId={TxId}] URL processing finished in {DurationMs} ms: Url=<{Url}>, Code={ResponseCode}", req.OriginalTxid, sw.ElapsedMilliseconds, req.Url, code);
            // Potentially log 'data' at Verbose level if needed, be careful with large responses
            // _log.Verbose("[TxId={TxId}] URL Response Data: {Data}", req.OriginalTxid, data);

            var oracleNodes = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
            var requestsToProcess = NativeContract.Oracle.GetRequestsByUrl(snapshot, req.Url).ToList();
            _log.Debug("[TxId={TxId}] Found {RequestCount} pending requests matching URL <{Url}>", req.OriginalTxid, requestsToProcess.Count, req.Url);

            foreach (var (requestId, request) in requestsToProcess)
            {
                _log.Information("[TxId={TxId}]-[ReqId={RequestId}] Processing specific request...", req.OriginalTxid, requestId);
                var result = Array.Empty<byte>();
                if (code == OracleResponseCode.Success)
                {
                    try
                    {
                        _log.Verbose("[TxId={TxId}]-[ReqId={RequestId}] Applying filter '{Filter}'", req.OriginalTxid, requestId, request.Filter);
                        sw.Restart();
                        result = Filter(data, request.Filter);
                        sw.Stop();
                        _log.Verbose("[TxId={TxId}]-[ReqId={RequestId}] Filtering finished in {DurationMs} ms. Result size: {ResultSize}", req.OriginalTxid, requestId, sw.ElapsedMilliseconds, result.Length);
                    }
                    catch (Exception ex)
                    {
                        code = OracleResponseCode.Error;
                        // Replace Log(...)
                        _log.Warning(ex, "[TxId={TxId}]-[ReqId={RequestId}] Filter '{Filter}' failed", req.OriginalTxid, requestId, request.Filter);
                    }
                }
                var response = new OracleResponse() { Id = requestId, Code = code, Result = result };
                Transaction responseTx = null;
                Transaction backupTx = null;
                try
                {
                    sw.Restart();
                    responseTx = CreateResponseTx(snapshot, request, response, oracleNodes, _system.Settings);
                    backupTx = CreateResponseTx(snapshot, request, new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = requestId, Result = Array.Empty<byte>() }, oracleNodes, _system.Settings, true);
                    sw.Stop();
                    if (responseTx == null || backupTx == null)
                        _log.Error("[TxId={TxId}]-[ReqId={RequestId}] Failed to create response transaction(s) after {DurationMs} ms", req.OriginalTxid, requestId, sw.ElapsedMilliseconds);
                    else
                        _log.Debug("[TxId={TxId}]-[ReqId={RequestId}] Created response tx {TxHash} and backup tx {BackupTxHash} in {DurationMs} ms",
                            req.OriginalTxid, requestId, responseTx.Hash, backupTx.Hash, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _log.Error(ex, "[TxId={TxId}]-[ReqId={RequestId}] Exception during response tx creation after {DurationMs} ms", req.OriginalTxid, requestId, sw.ElapsedMilliseconds);
                    continue; // Skip signing/sending if creation failed
                }

                if (responseTx == null || backupTx == null) continue; // Don't proceed if creation failed

                _log.Information("[TxId={TxId}]-[ReqId={RequestId}] Built response tx {TxHash} (Code={ResponseCode}, ResultSize={ResultSize}, VUB={ValidUntilBlock}), Backup tx {BackupTxHash} (VUB={BackupVUB})",
                    req.OriginalTxid, requestId, responseTx.Hash, code, result.Length, responseTx.ValidUntilBlock, backupTx.Hash, backupTx.ValidUntilBlock);

                List<Task> tasks = new List<Task>();
                ECPoint[] oraclePublicKeys = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height); // Reuse? Already have oracleNodes
                foreach (var account in wallet.GetAccounts())
                {
                    var keyPair = account.GetKey();
                    if (keyPair is null || !account.HasKey || account.Lock || !oraclePublicKeys.Contains(keyPair.PublicKey)) continue;

                    byte[] txSign = responseTx.Sign(keyPair, _system.Settings.Network);
                    byte[] backTxSign = backupTx.Sign(keyPair, _system.Settings.Network);

                    AddResponseTxSign(snapshot, requestId, keyPair.PublicKey, txSign, responseTx, backupTx, backTxSign);
                    tasks.Add(SendResponseSignatureAsync(requestId, txSign, keyPair));

                    _log.Debug("[TxId={TxId}]-[ReqId={RequestId}]-[Tx={TxHash}] Oracle node {OraclePub} signed response, submitting signature",
                        req.OriginalTxid, requestId, responseTx.Hash, keyPair.PublicKey);
                }
                await Task.WhenAll(tasks);
            }
        }

        private async Task ProcessRequestsAsync()
        {
            _log.Information("Oracle request processing loop started.");
            while (!cancelSource.IsCancellationRequested)
            {
                _log.Verbose("Oracle loop iteration start.");
                try
                {
                    using (var snapshot = _system.GetSnapshotCache())
                    {
                        _log.Verbose("Checking for pending oracle requests...");
                        SyncPendingQueue(snapshot);
                        var pendingRequests = NativeContract.Oracle.GetRequests(snapshot).ToList();
                        _log.Verbose("Found {RequestCount} total pending oracle requests in ledger", pendingRequests.Count);
                        int processedCount = 0;
                        foreach (var (id, request) in pendingRequests)
                        {
                            if (cancelSource.IsCancellationRequested) 
                            {
                                _log.Debug("Cancellation requested during request processing.");
                                break; // Exit inner loop
                            }
                            if (!finishedCache.ContainsKey(id) && (!pendingQueue.TryGetValue(id, out OracleTask task) || task.Tx is null))
                            {
                                _log.Debug("Found new/unprocessed oracle request {RequestId} for URL <{Url}>", id, request.Url);
                                await ProcessRequestAsync(snapshot, request); // Await the processing
                                processedCount++;
                            }
                        }
                        if (processedCount == 0) _log.Verbose("No new oracle requests to process this iteration.");
                    }
                }
                catch (Exception ex)
                {
                    // Catch OperationCanceledException specifically if it bubbles up unexpectedly
                    if (ex is OperationCanceledException oce)
                    {
                        _log.Information(oce, "OperationCanceledException caught in main loop body. Stopping loop.");
                        break;
                    }
                    _log.Error(ex, "Unhandled exception in Oracle request processing loop.");
                    // Avoid tight loop on persistent errors
                     try
                     {
                         _log.Verbose("Delaying after error...");
                         await Task.Delay(5000, cancelSource.Token); 
                     }
                     catch (OperationCanceledException)
                     {
                         _log.Information("Delay cancelled during error handling, stopping loop.");
                         break; // Exit loop if delay is cancelled
                     }
                }

                if (cancelSource.IsCancellationRequested) 
                {
                    _log.Debug("Cancellation requested before final delay.");
                    break; // Check again before delay
                }
                _log.Verbose("Oracle processing loop delay (500ms)...");
                 try
                 {
                     await Task.Delay(500, cancelSource.Token);
                     _log.Verbose("Loop delay completed.");
                 }
                 catch (OperationCanceledException)
                 {
                     _log.Information("Delay cancelled, stopping loop.");
                     break; // Exit loop if delay is cancelled
                 }
                 _log.Verbose("Oracle loop iteration end.");
            }

            status = OracleStatus.Stopped; 
            _log.Information("Oracle request processing loop stopped cleanly.");
        }

        private void SyncPendingQueue(DataCache snapshot)
        {
            var onChainRequests = NativeContract.Oracle.GetRequests(snapshot).Select(r => r.Item1).ToHashSet();
            var requestsToRemove = pendingQueue.Keys.Where(k => !onChainRequests.Contains(k)).ToList();

            if (requestsToRemove.Count > 0)
            {
                _log.Information("Syncing pending queue: Removing {Count} requests not found on-chain", requestsToRemove.Count);
                foreach (var reqId in requestsToRemove)
                {
                    if (pendingQueue.TryRemove(reqId, out _))
                        _log.Debug("Removed orphaned pending request {RequestId}", reqId);
                }
            }
            else
            {
                _log.Verbose("Syncing pending queue: No orphaned requests found.");
            }
        }

        private async Task<(OracleResponseCode, string)> ProcessUrlAsync(string url)
        {
            _log.Debug("Processing URL <{Url}>", url);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _log.Warning("Invalid URL format: <{Url}>", url);
                return (OracleResponseCode.Error, $"Invalid url:<{url}>");
            }
            if (!protocols.TryGetValue(uri.Scheme, out IOracleProtocol protocol))
            {
                _log.Warning("Protocol not supported for URL <{Url}> (Scheme: {Scheme})", url, uri.Scheme);
                return (OracleResponseCode.ProtocolNotSupported, $"Invalid Protocol:<{url}>");
            }

            using CancellationTokenSource ctsTimeout = new(Settings.Default.MaxOracleTimeout);
            using CancellationTokenSource ctsLinked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, ctsTimeout.Token);

            try
            {
                _log.Verbose("Calling protocol handler {ProtocolType} for URL <{Url}>", protocol.GetType().Name, url);
                var result = await protocol.ProcessAsync(uri, ctsLinked.Token);
                _log.Verbose("Protocol handler for URL <{Url}> returned code {ResponseCode}", url, result.Item1);
                return result;
            }
            catch (OperationCanceledException) when (ctsTimeout.IsCancellationRequested)
            {
                _log.Warning("Timeout processing URL <{Url}> after {TimeoutMs} ms", url, Settings.Default.MaxOracleTimeout);
                return (OracleResponseCode.Timeout, "Timeout");
            }
            catch (OperationCanceledException) when (cancelSource.IsCancellationRequested)
            {
                _log.Information("Processing cancelled for URL <{Url}>", url);
                return (OracleResponseCode.Error, "Cancelled");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception processing URL <{Url}>", url);
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
                Signers = [
                    new(){ Account = NativeContract.Oracle.Hash, Scopes = WitnessScope.None },
                    new(){ Account = oracleSignContract.ScriptHash, Scopes = WitnessScope.None },
                ],
                Attributes = [response],
                Script = OracleResponse.FixedScript,
                Witnesses = new Witness[2]
            };

            var witnessDict = new Dictionary<UInt160, Witness>
            {
                [oracleSignContract.ScriptHash] = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = oracleSignContract.Script,
                },
                [NativeContract.Oracle.Hash] = Witness.Empty,
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

            int sizeInv = 66 * m;
            int size = Transaction.HeaderSize + tx.Signers.GetVarSize() + tx.Script.GetVarSize()
                + hashes.Length.GetVarSize() + witnessDict[NativeContract.Oracle.Hash].Size
                + sizeInv.GetVarSize() + sizeInv + oracleSignContract.Script.GetVarSize();

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
            _log.Debug("Adding response signature for RequestId={RequestId}, Oracle={OraclePub}", requestId, oraclePub);
            var task = pendingQueue.GetOrAdd(requestId, _ =>
            {
                _log.Debug("Creating new OracleTask for RequestId={RequestId}", requestId);
                return new OracleTask
                {
                    Id = requestId,
                    Request = NativeContract.Oracle.GetRequest(snapshot, requestId),
                    Signs = new ConcurrentDictionary<ECPoint, byte[]>(),
                    BackupSigns = new ConcurrentDictionary<ECPoint, byte[]>()
                };
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
            {
                _log.Verbose("Signature verified for main response tx {TxHash}, adding to task {RequestId}", task.Tx.Hash, requestId);
                task.Signs.TryAdd(oraclePub, sign);
            }
            else if (task.BackupTx != null && Crypto.VerifySignature(task.BackupTx.GetSignData(_system.Settings.Network), sign, oraclePub))
            {
                _log.Verbose("Signature verified for backup response tx {TxHash}, adding to task {RequestId}", task.BackupTx.Hash, requestId);
                task.BackupSigns.TryAdd(oraclePub, sign);
            }
            else
            {
                _log.Error("Invalid signature provided by {OraclePub} for request {RequestId} (neither main nor backup tx matched)", oraclePub, requestId);
                throw new RpcException(RpcErrorFactory.InvalidSignature($"Invalid oracle response transaction signature from '{oraclePub}'."));
            }

            if (CheckTxSign(snapshot, task.Tx, task.Signs) || (task.BackupTx != null && CheckTxSign(snapshot, task.BackupTx, task.BackupSigns)))
            {
                _log.Information("Threshold met for RequestId={RequestId}, marking as finished and removing from pending queue.", requestId);
                finishedCache.TryAdd(requestId, TimeProvider.Current.UtcNow);
                pendingQueue.TryRemove(requestId, out _);
            }
            else
            {
                _log.Debug("Signature threshold not yet met for RequestId={RequestId} (MainSigns={MainCount}, BackupSigns={BackupCount})",
                    requestId, task.Signs.Count, task.BackupSigns.Count);
            }
        }

        public static byte[] Filter(string input, string filterArgs)
        {
            if (string.IsNullOrEmpty(filterArgs))
                return input.ToStrictUtf8Bytes();

            JToken beforeObject = JToken.Parse(input);
            JArray afterObjects = beforeObject.JsonPath(filterArgs);
            return afterObjects.ToByteArray(false);
        }

        private bool CheckTxSign(DataCache snapshot, Transaction tx, ConcurrentDictionary<ECPoint, byte[]> OracleSigns)
        {
            uint height = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
            if (tx.ValidUntilBlock <= height)
            {
                _log.Warning("Cannot submit oracle response tx {TxHash}: Expired (VUB={VUB}, CurrentHeight={Height})", tx.Hash, tx.ValidUntilBlock, height);
                return false;
            }
            ECPoint[] oraclesNodes = NativeContract.RoleManagement.GetDesignatedByRole(snapshot, Role.Oracle, height);
            int neededThreshold = oraclesNodes.Length - (oraclesNodes.Length - 1) / 3;
            _log.Debug("Checking signature threshold for tx {TxHash}: Have={HaveCount}, Need={NeedCount}", tx.Hash, OracleSigns.Count, neededThreshold);
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

                // Replace Log(...)
                _log.Information("Signature threshold met for tx {TxHash}, relaying transaction.", tx.Hash);
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

        private static bool CheckOracleAccount(ISigner signer, ECPoint[] oracles)
        {
            return signer is not null && oracles.Any(p => signer.ContainsSignable(p));
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

