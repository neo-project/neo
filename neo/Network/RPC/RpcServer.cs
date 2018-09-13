using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Neo.Core;
using Neo.IO;
using Neo.IO.Json;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Network.RPC
{
    public class RpcServer : IDisposable
    {
        protected readonly LocalNode LocalNode;
        private IWebHost host;

        public RpcServer(LocalNode localNode)
        {
            this.LocalNode = localNode;
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

        private static JObject GetInvokeResult(byte[] script)
        {
            ApplicationEngine engine = ApplicationEngine.Run(script);
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
            return json;
        }

        protected virtual JObject Process(string method, JArray _params)
        {
            switch (method)
            {
                case "getaccountstate":
                    {
                        UInt160 script_hash = Wallet.ToScriptHash(_params[0].AsString());
                        AccountState account = Blockchain.Default.GetAccountState(script_hash) ?? new AccountState(script_hash);
                        return account.ToJson();
                    }
                case "getassetstate":
                    {
                        UInt256 asset_id = UInt256.Parse(_params[0].AsString());
                        AssetState asset = Blockchain.Default.GetAssetState(asset_id);
                        return asset?.ToJson() ?? throw new RpcException(-100, "Unknown asset");
                    }
                case "getbestblockhash":
                    return Blockchain.Default.CurrentBlockHash.ToString();
                case "getblock":
                    {
                        Block block;
                        if (_params[0] is JNumber)
                        {
                            uint index = (uint)_params[0].AsNumber();
                            block = Blockchain.Default.GetBlock(index);
                        }
                        else
                        {
                            UInt256 hash = UInt256.Parse(_params[0].AsString());
                            block = Blockchain.Default.GetBlock(hash);
                        }
                        if (block == null)
                            throw new RpcException(-100, "Unknown block");
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        if (verbose)
                        {
                            JObject json = block.ToJson();
                            json["confirmations"] = Blockchain.Default.Height - block.Index + 1;
                            UInt256 hash = Blockchain.Default.GetNextBlockHash(block.Hash);
                            if (hash != null)
                                json["nextblockhash"] = hash.ToString();
                            return json;
                        }

                        return block.ToArray().ToHexString();
                    }
                case "getblockcount":
                    return Blockchain.Default.Height + 1;
                case "getblockheader":
                    {
                        Header header;
                        if (_params[0] is JNumber)
                        {
                            uint height = (uint)_params[0].AsNumber();
                            header = Blockchain.Default.GetHeader(height);
                        }
                        else
                        {
                            UInt256 hash = UInt256.Parse(_params[0].AsString());
                            header = Blockchain.Default.GetHeader(hash);
                        }
                        if (header == null)
                            throw new RpcException(-100, "Unknown block");

                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        if (verbose)
                        {
                            JObject json = header.ToJson();
                            json["confirmations"] = Blockchain.Default.Height - header.Index + 1;
                            UInt256 hash = Blockchain.Default.GetNextBlockHash(header.Hash);
                            if (hash != null)
                                json["nextblockhash"] = hash.ToString();
                            return json;
                        }

                        return header.ToArray().ToHexString();
                    }
                case "getblockhash":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        if (height <= Blockchain.Default.Height)
                        {
                            return Blockchain.Default.GetBlockHash(height).ToString();
                        }

                        throw new RpcException(-100, "Invalid Height");
                    }
                case "getblocksysfee":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        if (height <= Blockchain.Default.Height)
                        {
                            return Blockchain.Default.GetSysFeeAmount(height).ToString();
                        }

                        throw new RpcException(-100, "Invalid Height");
                    }
                case "getconnectioncount":
                    return LocalNode.RemoteNodeCount;
                case "getcontractstate":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        ContractState contract = Blockchain.Default.GetContract(script_hash);
                        return contract?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
                    }
                case "getrawmempool":
                    return new JArray(LocalNode.GetMemoryPool().Select(p => (JObject)p.Hash.ToString()));
                case "getrawtransaction":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        int height = -1;
                        Transaction tx = LocalNode.GetTransaction(hash);
                        if (tx == null)
                            tx = Blockchain.Default.GetTransaction(hash, out height);
                        if (tx == null)
                            throw new RpcException(-100, "Unknown transaction");
                        if (verbose)
                        {
                            JObject json = tx.ToJson();
                            if (height >= 0)
                            {
                                Header header = Blockchain.Default.GetHeader((uint)height);
                                json["blockhash"] = header.Hash.ToString();
                                json["confirmations"] = Blockchain.Default.Height - header.Index + 1;
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
                        StorageItem item = Blockchain.Default.GetStorageItem(new StorageKey
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
                        return Blockchain.Default.GetUnspent(hash, index)?.ToJson(index);
                    }
                case "getvalidators":
                    {
                        var validators = Blockchain.Default.GetValidators();
                        return Blockchain.Default.GetEnrollments().Select(p =>
                        {
                            JObject validator = new JObject();
                            validator["publickey"] = p.PublicKey.ToString();
                            validator["votes"] = p.Votes.ToString();
                            validator["active"] = validators.Contains(p.PublicKey);
                            return validator;
                        }).ToArray();
                    }
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
                case "sendrawtransaction":
                    {
                        Transaction tx = Transaction.DeserializeFrom(_params[0].AsString().HexToBytes());
                        return LocalNode.Relay(tx);
                    }
                case "submitblock":
                    {
                        Block block = _params[0].AsString().HexToBytes().AsSerializable<Block>();
                        return LocalNode.Relay(block);
                    }
                case "validateaddress":
                    {
                        JObject json = new JObject();
                        UInt160 scriptHash;
                        try
                        {
                            scriptHash = Wallet.ToScriptHash(_params[0].AsString());
                        }
                        catch
                        {
                            scriptHash = null;
                        }
                        json["address"] = _params[0];
                        json["isvalid"] = scriptHash != null;
                        return json;
                    }
                case "getpeers":
                    {
                        JObject json = new JObject();

                        {
                            JArray unconnectedPeers = new JArray();
                            foreach (IPEndPoint peer in LocalNode.GetUnconnectedPeers())
                            {
                                JObject peerJson = new JObject();
                                peerJson["address"] = peer.Address.ToString();
                                peerJson["port"] = peer.Port;
                                unconnectedPeers.Add(peerJson);
                            }
                            json["unconnected"] = unconnectedPeers;
                        }

                        {
                            JArray badPeers = new JArray();
                            foreach (IPEndPoint peer in LocalNode.GetBadPeers())
                            {
                                JObject peerJson = new JObject();
                                peerJson["address"] = peer.Address.ToString();
                                peerJson["port"] = peer.Port;
                                badPeers.Add(peerJson);
                            }
                            json["bad"] = badPeers;
                        }

                        {
                            JArray connectedPeers = new JArray();
                            foreach (RemoteNode node in LocalNode.GetRemoteNodes())
                            {
                                JObject peerJson = new JObject();
                                peerJson["address"] = node.RemoteEndpoint.Address.ToString();
                                peerJson["port"] = node.ListenerEndpoint?.Port ?? 0;
                                connectedPeers.Add(peerJson);
                            }
                            json["connected"] = connectedPeers;
                        }

                        return json;
                    }
                case "getversion":
                    {
                        JObject json = new JObject();
                        json["port"] = LocalNode.Port;
                        json["nonce"] = LocalNode.Nonce;
                        json["useragent"] = LocalNode.UserAgent;
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
                    request["id"] = double.Parse(id);
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
                foreach (RpcPlugin plugin in RpcPlugin.Instances)
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

        public void Start(int port, string sslCert = null, string password = null)
        {
            host = new WebHostBuilder().UseKestrel(options => options.Listen(IPAddress.Any, port, listenOptions =>
            {
                if (!string.IsNullOrEmpty(sslCert))
                    listenOptions.UseHttps(sslCert, password);
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
