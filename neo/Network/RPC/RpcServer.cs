using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Network.RPC
{
    public sealed class RpcServer : IDisposable
    {
        private readonly NeoSystem system;
        private Wallet wallet;
        private IWebHost host;
        private Fixed8 maxGasInvoke;

        public RpcServer(NeoSystem system, Wallet wallet = null, Fixed8 maxGasInvoke = default(Fixed8))
        {
            this.system = system;
            this.wallet = wallet;
            this.maxGasInvoke = maxGasInvoke;
        }

        private static JObject CreateErrorResponse(JObject id, int code, string message, JObject data = null)
        {
            JObject response = CreateResponse(id);
            response["error"] = new JObject();
            response["error"]["code"] = code;
            response["error"]["message"] = message;
            if (data != null)
                response["error"]["data"] = data;
            return response;
        }

        private static JObject CreateResponse(JObject id)
        {
            JObject response = new JObject();
            response["jsonrpc"] = "2.0";
            response["id"] = id;
            return response;
        }

        public void Dispose()
        {
            if (host != null)
            {
                host.Dispose();
                host = null;
            }
        }

        private JObject GetInvokeResult(byte[] script)
        {
            ApplicationEngine engine = ApplicationEngine.Run(script, extraGAS: maxGasInvoke);
            JObject json = new JObject();
            json["script"] = script.ToHexString();
            json["state"] = engine.State;
            json["gas_consumed"] = engine.GasConsumed.ToString();
            try
            {
                json["stack"] = new JArray(engine.ResultStack.Select(p => p.ToParameter().ToJson()));
            }
            catch (InvalidOperationException)
            {
                json["stack"] = "error: recursive reference";
            }
            if (wallet != null)
            {
                InvocationTransaction tx = new InvocationTransaction
                {
                    Version = 1,
                    Script = json["script"].AsString().HexToBytes(),
                    Gas = Fixed8.Parse(json["gas_consumed"].AsString())
                };
                tx.Gas -= Fixed8.FromDecimal(10);
                if (tx.Gas < Fixed8.Zero) tx.Gas = Fixed8.Zero;
                tx.Gas = tx.Gas.Ceiling();
                tx = wallet.MakeTransaction(tx);
                if (tx != null)
                {
                    ContractParametersContext context = new ContractParametersContext(tx);
                    wallet.Sign(context);
                    if (context.Completed)
                        tx.Witnesses = context.GetWitnesses();
                    else
                        tx = null;
                }
                json["tx"] = tx?.ToArray().ToHexString();
            }
            return json;
        }

        private static JObject GetRelayResult(RelayResultReason reason)
        {
            switch (reason)
            {
                case RelayResultReason.Succeed:
                    return true;
                case RelayResultReason.AlreadyExists:
                    throw new RpcException(-501, "Block or transaction already exists and cannot be sent repeatedly.");
                case RelayResultReason.OutOfMemory:
                    throw new RpcException(-502, "The memory pool is full and no more transactions can be sent.");
                case RelayResultReason.UnableToVerify:
                    throw new RpcException(-503, "The block cannot be validated.");
                case RelayResultReason.Invalid:
                    throw new RpcException(-504, "Block or transaction validation failed.");
                default:
                    throw new RpcException(-500, "Unkown error.");
            }
        }

        public void OpenWallet(Wallet wallet)
        {
            this.wallet = wallet;
        }

        private JObject Process(string method, JArray _params)
        {
            switch (method)
            {
                case "dumpprivkey":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        UInt160 scriptHash = _params[0].AsString().ToScriptHash();
                        WalletAccount account = wallet.GetAccount(scriptHash);
                        return account.GetKey().Export();
                    }
                case "getaccountstate":
                    {
                        UInt160 script_hash = _params[0].AsString().ToScriptHash();
                        AccountState account = Blockchain.Singleton.Store.GetAccounts().TryGet(script_hash) ?? new AccountState(script_hash);
                        return account.ToJson();
                    }
                case "getassetstate":
                    {
                        UInt256 asset_id = UInt256.Parse(_params[0].AsString());
                        AssetState asset = Blockchain.Singleton.Store.GetAssets().TryGet(asset_id);
                        return asset?.ToJson() ?? throw new RpcException(-100, "Unknown asset");
                    }
                case "getbalance":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied.");
                    else
                    {
                        JObject json = new JObject();
                        switch (UIntBase.Parse(_params[0].AsString()))
                        {
                            case UInt160 asset_id_160: //NEP-5 balance
                                json["balance"] = wallet.GetAvailable(asset_id_160).ToString();
                                break;
                            case UInt256 asset_id_256: //Global Assets balance
                                IEnumerable<Coin> coins = wallet.GetCoins().Where(p => !p.State.HasFlag(CoinState.Spent) && p.Output.AssetId.Equals(asset_id_256));
                                json["balance"] = coins.Sum(p => p.Output.Value).ToString();
                                json["confirmed"] = coins.Where(p => p.State.HasFlag(CoinState.Confirmed)).Sum(p => p.Output.Value).ToString();
                                break;
                        }
                        return json;
                    }
                case "getbestblockhash":
                    return Blockchain.Singleton.CurrentBlockHash.ToString();
                case "getblock":
                    {
                        Block block;
                        if (_params[0] is JNumber)
                        {
                            uint index = (uint)_params[0].AsNumber();
                            block = Blockchain.Singleton.Store.GetBlock(index);
                        }
                        else
                        {
                            UInt256 hash = UInt256.Parse(_params[0].AsString());
                            block = Blockchain.Singleton.Store.GetBlock(hash);
                        }
                        if (block == null)
                            throw new RpcException(-100, "Unknown block");
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        if (verbose)
                        {
                            JObject json = block.ToJson();
                            json["confirmations"] = Blockchain.Singleton.Height - block.Index + 1;
                            UInt256 hash = Blockchain.Singleton.Store.GetNextBlockHash(block.Hash);
                            if (hash != null)
                                json["nextblockhash"] = hash.ToString();
                            return json;
                        }
                        return block.ToArray().ToHexString();
                    }
                case "getblockcount":
                    return Blockchain.Singleton.Height + 1;
                case "getblockhash":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        if (height <= Blockchain.Singleton.Height)
                        {
                            return Blockchain.Singleton.GetBlockHash(height).ToString();
                        }
                        throw new RpcException(-100, "Invalid Height");
                    }
                case "getblockheader":
                    {
                        Header header;
                        if (_params[0] is JNumber)
                        {
                            uint height = (uint)_params[0].AsNumber();
                            header = Blockchain.Singleton.Store.GetHeader(height);
                        }
                        else
                        {
                            UInt256 hash = UInt256.Parse(_params[0].AsString());
                            header = Blockchain.Singleton.Store.GetHeader(hash);
                        }
                        if (header == null)
                            throw new RpcException(-100, "Unknown block");

                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        if (verbose)
                        {
                            JObject json = header.ToJson();
                            json["confirmations"] = Blockchain.Singleton.Height - header.Index + 1;
                            UInt256 hash = Blockchain.Singleton.Store.GetNextBlockHash(header.Hash);
                            if (hash != null)
                                json["nextblockhash"] = hash.ToString();
                            return json;
                        }

                        return header.ToArray().ToHexString();
                    }
                case "getblocksysfee":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        if (height <= Blockchain.Singleton.Height)
                        {
                            return Blockchain.Singleton.Store.GetSysFeeAmount(height).ToString();
                        }
                        throw new RpcException(-100, "Invalid Height");
                    }
                case "getconnectioncount":
                    return LocalNode.Singleton.ConnectedCount;
                case "getcontractstate":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        ContractState contract = Blockchain.Singleton.Store.GetContracts().TryGet(script_hash);
                        return contract?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
                    }
                case "getnewaddress":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        WalletAccount account = wallet.CreateAccount();
                        if (wallet is NEP6Wallet nep6)
                            nep6.Save();
                        return account.Address;
                    }
                case "getpeers":
                    {
                        JObject json = new JObject();
                        json["unconnected"] = new JArray(LocalNode.Singleton.GetUnconnectedPeers().Select(p =>
                        {
                            JObject peerJson = new JObject();
                            peerJson["address"] = p.Address.ToString();
                            peerJson["port"] = p.Port;
                            return peerJson;
                        }));
                        json["bad"] = new JArray(); //badpeers has been removed
                        json["connected"] = new JArray(LocalNode.Singleton.GetRemoteNodes().Select(p =>
                        {
                            JObject peerJson = new JObject();
                            peerJson["address"] = p.Remote.Address.ToString();
                            peerJson["port"] = p.ListenerPort;
                            return peerJson;
                        }));
                        return json;
                    }
                case "getrawmempool":
                    return new JArray(Blockchain.Singleton.GetMemoryPool().Select(p => (JObject)p.Hash.ToString()));
                case "getrawtransaction":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        Transaction tx = Blockchain.Singleton.GetTransaction(hash);
                        if (tx == null)
                            throw new RpcException(-100, "Unknown transaction");
                        if (verbose)
                        {
                            JObject json = tx.ToJson();
                            uint? height = Blockchain.Singleton.Store.GetTransactions().TryGet(hash)?.BlockIndex;
                            if (height != null)
                            {
                                Header header = Blockchain.Singleton.Store.GetHeader((uint)height);
                                json["blockhash"] = header.Hash.ToString();
                                json["confirmations"] = Blockchain.Singleton.Height - header.Index + 1;
                                json["blocktime"] = header.Timestamp;
                            }
                            return json;
                        }
                        return tx.ToArray().ToHexString();
                    }
                case "getstorage":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        byte[] key = _params[1].AsString().HexToBytes();
                        StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
                        {
                            ScriptHash = script_hash,
                            Key = key
                        }) ?? new StorageItem();
                        return item.Value?.ToHexString();
                    }
                case "gettxout":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        ushort index = (ushort)_params[1].AsNumber();
                        return Blockchain.Singleton.Store.GetUnspent(hash, index)?.ToJson(index);
                    }
                case "getvalidators":
                    using (Snapshot snapshot = Blockchain.Singleton.GetSnapshot())
                    {
                        var validators = snapshot.GetValidators();
                        return snapshot.GetEnrollments().Select(p =>
                        {
                            JObject validator = new JObject();
                            validator["publickey"] = p.PublicKey.ToString();
                            validator["votes"] = p.Votes.ToString();
                            validator["active"] = validators.Contains(p.PublicKey);
                            return validator;
                        }).ToArray();
                    }
                case "getversion":
                    {
                        JObject json = new JObject();
                        json["port"] = LocalNode.Singleton.ListenerPort;
                        json["nonce"] = LocalNode.Nonce;
                        json["useragent"] = LocalNode.UserAgent;
                        return json;
                    }
                case "getwalletheight":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied.");
                    else
                        return (wallet.WalletHeight > 0) ? wallet.WalletHeight - 1 : 0;
                case "invoke":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        ContractParameter[] parameters = ((JArray)_params[1]).Select(p => ContractParameter.FromJson(p)).ToArray();
                        byte[] script;
                        using (ScriptBuilder sb = new ScriptBuilder())
                        {
                            script = sb.EmitAppCall(script_hash, parameters).ToArray();
                        }
                        return GetInvokeResult(script);
                    }
                case "invokefunction":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        string operation = _params[1].AsString();
                        ContractParameter[] args = _params.Count >= 3 ? ((JArray)_params[2]).Select(p => ContractParameter.FromJson(p)).ToArray() : new ContractParameter[0];
                        byte[] script;
                        using (ScriptBuilder sb = new ScriptBuilder())
                        {
                            script = sb.EmitAppCall(script_hash, operation, args).ToArray();
                        }
                        return GetInvokeResult(script);
                    }
                case "invokescript":
                    {
                        byte[] script = _params[0].AsString().HexToBytes();
                        return GetInvokeResult(script);
                    }
                case "listaddress":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied.");
                    else
                        return wallet.GetAccounts().Select(p =>
                        {
                            JObject account = new JObject();
                            account["address"] = p.Address;
                            account["haskey"] = p.HasKey;
                            account["label"] = p.Label;
                            account["watchonly"] = p.WatchOnly;
                            return account;
                        }).ToArray();
                case "sendfrom":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        UIntBase assetId = UIntBase.Parse(_params[0].AsString());
                        AssetDescriptor descriptor = new AssetDescriptor(assetId);
                        UInt160 from = _params[1].AsString().ToScriptHash();
                        UInt160 to = _params[2].AsString().ToScriptHash();
                        BigDecimal value = BigDecimal.Parse(_params[3].AsString(), descriptor.Decimals);
                        if (value.Sign <= 0)
                            throw new RpcException(-32602, "Invalid params");
                        Fixed8 fee = _params.Count >= 5 ? Fixed8.Parse(_params[4].AsString()) : Fixed8.Zero;
                        if (fee < Fixed8.Zero)
                            throw new RpcException(-32602, "Invalid params");
                        UInt160 change_address = _params.Count >= 6 ? _params[5].AsString().ToScriptHash() : null;
                        Transaction tx = wallet.MakeTransaction(null, new[]
                        {
                            new TransferOutput
                            {
                                AssetId = assetId,
                                Value = value,
                                ScriptHash = to
                            }
                        }, from: from, change_address: change_address, fee: fee);
                        if (tx == null)
                            throw new RpcException(-300, "Insufficient funds");
                        ContractParametersContext context = new ContractParametersContext(tx);
                        wallet.Sign(context);
                        if (context.Completed)
                        {
                            tx.Witnesses = context.GetWitnesses();
                            wallet.ApplyTransaction(tx);
                            system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
                            return tx.ToJson();
                        }
                        else
                        {
                            return context.ToJson();
                        }
                    }
                case "sendmany":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        JArray to = (JArray)_params[0];
                        if (to.Count == 0)
                            throw new RpcException(-32602, "Invalid params");
                        TransferOutput[] outputs = new TransferOutput[to.Count];
                        for (int i = 0; i < to.Count; i++)
                        {
                            UIntBase asset_id = UIntBase.Parse(to[i]["asset"].AsString());
                            AssetDescriptor descriptor = new AssetDescriptor(asset_id);
                            outputs[i] = new TransferOutput
                            {
                                AssetId = asset_id,
                                Value = BigDecimal.Parse(to[i]["value"].AsString(), descriptor.Decimals),
                                ScriptHash = to[i]["address"].AsString().ToScriptHash()
                            };
                            if (outputs[i].Value.Sign <= 0)
                                throw new RpcException(-32602, "Invalid params");
                        }
                        Fixed8 fee = _params.Count >= 2 ? Fixed8.Parse(_params[1].AsString()) : Fixed8.Zero;
                        if (fee < Fixed8.Zero)
                            throw new RpcException(-32602, "Invalid params");
                        UInt160 change_address = _params.Count >= 3 ? _params[2].AsString().ToScriptHash() : null;
                        Transaction tx = wallet.MakeTransaction(null, outputs, change_address: change_address, fee: fee);
                        if (tx == null)
                            throw new RpcException(-300, "Insufficient funds");
                        ContractParametersContext context = new ContractParametersContext(tx);
                        wallet.Sign(context);
                        if (context.Completed)
                        {
                            tx.Witnesses = context.GetWitnesses();
                            wallet.ApplyTransaction(tx);
                            system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
                            return tx.ToJson();
                        }
                        else
                        {
                            return context.ToJson();
                        }
                    }
                case "sendrawtransaction":
                    {
                        Transaction tx = Transaction.DeserializeFrom(_params[0].AsString().HexToBytes());
                        RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(tx).Result;
                        return GetRelayResult(reason);
                    }
                case "sendtoaddress":
                    if (wallet == null)
                        throw new RpcException(-400, "Access denied");
                    else
                    {
                        UIntBase assetId = UIntBase.Parse(_params[0].AsString());
                        AssetDescriptor descriptor = new AssetDescriptor(assetId);
                        UInt160 scriptHash = _params[1].AsString().ToScriptHash();
                        BigDecimal value = BigDecimal.Parse(_params[2].AsString(), descriptor.Decimals);
                        if (value.Sign <= 0)
                            throw new RpcException(-32602, "Invalid params");
                        Fixed8 fee = _params.Count >= 4 ? Fixed8.Parse(_params[3].AsString()) : Fixed8.Zero;
                        if (fee < Fixed8.Zero)
                            throw new RpcException(-32602, "Invalid params");
                        UInt160 change_address = _params.Count >= 5 ? _params[4].AsString().ToScriptHash() : null;
                        Transaction tx = wallet.MakeTransaction(null, new[]
                        {
                            new TransferOutput
                            {
                                AssetId = assetId,
                                Value = value,
                                ScriptHash = scriptHash
                            }
                        }, change_address: change_address, fee: fee);
                        if (tx == null)
                            throw new RpcException(-300, "Insufficient funds");
                        ContractParametersContext context = new ContractParametersContext(tx);
                        wallet.Sign(context);
                        if (context.Completed)
                        {
                            tx.Witnesses = context.GetWitnesses();
                            wallet.ApplyTransaction(tx);
                            system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });
                            return tx.ToJson();
                        }
                        else
                        {
                            return context.ToJson();
                        }
                    }
                case "submitblock":
                    {
                        Block block = _params[0].AsString().HexToBytes().AsSerializable<Block>();
                        RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(block).Result;
                        return GetRelayResult(reason);
                    }
                case "validateaddress":
                    {
                        JObject json = new JObject();
                        UInt160 scriptHash;
                        try
                        {
                            scriptHash = _params[0].AsString().ToScriptHash();
                        }
                        catch
                        {
                            scriptHash = null;
                        }
                        json["address"] = _params[0];
                        json["isvalid"] = scriptHash != null;
                        return json;
                    }
                default:
                    throw new RpcException(-32601, "Method not found");
            }
        }

        private async Task ProcessAsync(HttpContext context)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";
            if (context.Request.Method != "GET" && context.Request.Method != "POST") return;
            JObject request = null;
            if (context.Request.Method == "GET")
            {
                string jsonrpc = context.Request.Query["jsonrpc"];
                string id = context.Request.Query["id"];
                string method = context.Request.Query["method"];
                string _params = context.Request.Query["params"];
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(_params))
                {
                    try
                    {
                        _params = Encoding.UTF8.GetString(Convert.FromBase64String(_params));
                    }
                    catch (FormatException) { }
                    request = new JObject();
                    if (!string.IsNullOrEmpty(jsonrpc))
                        request["jsonrpc"] = jsonrpc;
                    request["id"] = id;
                    request["method"] = method;
                    request["params"] = JObject.Parse(_params);
                }
            }
            else if (context.Request.Method == "POST")
            {
                using (StreamReader reader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        request = JObject.Parse(reader);
                    }
                    catch (FormatException) { }
                }
            }
            JObject response;
            if (request == null)
            {
                response = CreateErrorResponse(null, -32700, "Parse error");
            }
            else if (request is JArray array)
            {
                if (array.Count == 0)
                {
                    response = CreateErrorResponse(request["id"], -32600, "Invalid Request");
                }
                else
                {
                    response = array.Select(p => ProcessRequest(context, p)).Where(p => p != null).ToArray();
                }
            }
            else
            {
                response = ProcessRequest(context, request);
            }
            if (response == null || (response as JArray)?.Count == 0) return;
            context.Response.ContentType = "application/json-rpc";
            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
        }

        private JObject ProcessRequest(HttpContext context, JObject request)
        {
            if (!request.ContainsProperty("id")) return null;
            if (!request.ContainsProperty("method") || !request.ContainsProperty("params") || !(request["params"] is JArray))
            {
                return CreateErrorResponse(request["id"], -32600, "Invalid Request");
            }
            JObject result = null;
            try
            {
                string method = request["method"].AsString();
                JArray _params = (JArray)request["params"];
                foreach (IRpcPlugin plugin in Plugin.RpcPlugins)
                {
                    result = plugin.OnProcess(context, method, _params);
                    if (result != null) break;
                }
                if (result == null)
                    result = Process(method, _params);
            }
            catch (Exception ex)
            {
#if DEBUG
                return CreateErrorResponse(request["id"], ex.HResult, ex.Message, ex.StackTrace);
#else
                return CreateErrorResponse(request["id"], ex.HResult, ex.Message);
#endif
            }
            JObject response = CreateResponse(request["id"]);
            response["result"] = result;
            return response;
        }

        public void Start(IPAddress bindAddress, int port, string sslCert = null, string password = null, string[] trustedAuthorities = null)
        {
            host = new WebHostBuilder().UseKestrel(options => options.Listen(bindAddress, port, listenOptions =>
            {
                if (string.IsNullOrEmpty(sslCert)) return;
                listenOptions.UseHttps(sslCert, password, httpsConnectionAdapterOptions =>
                {
                    if (trustedAuthorities is null || trustedAuthorities.Length == 0)
                        return;
                    httpsConnectionAdapterOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpsConnectionAdapterOptions.ClientCertificateValidation = (cert, chain, err) =>
                    {
                        if (err != SslPolicyErrors.None)
                            return false;
                        X509Certificate2 authority = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                        return trustedAuthorities.Contains(authority.Thumbprint);
                    };
                });
            }))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            host.Start();
        }
    }
}
