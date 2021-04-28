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
using Neo.Trie.MPT;
using Neo.VM;
using Neo.Wallets;

namespace Neo.Network.RPC
{
    public sealed class RpcServer : IDisposable
    {
        private class CheckWitnessHashes : IVerifiable
        {
            private readonly UInt160[] _scriptHashesForVerifying;
            public Witness[] Witnesses { get; set; }
            public int Size { get; }

            public CheckWitnessHashes(UInt160[] scriptHashesForVerifying)
            {
                _scriptHashesForVerifying = scriptHashesForVerifying;
            }

            public void Serialize(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(BinaryReader reader)
            {
                throw new NotImplementedException();
            }

            public void DeserializeUnsigned(BinaryReader reader)
            {
                throw new NotImplementedException();
            }

            public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
            {
                return _scriptHashesForVerifying;
            }

            public void SerializeUnsigned(BinaryWriter writer)
            {
                throw new NotImplementedException();
            }

            public byte[] GetMessage()
            {
                throw new NotImplementedException();
            }
        }

        public Wallet Wallet { get; set; }
        public Fixed8 ExtraGasInvoke { get; }
        public int MaxConcurrentConnections { get; }

        private IWebHost host;
        private readonly NeoSystem system;

        public RpcServer(NeoSystem system, Wallet wallet = null, Fixed8 extraGasInvoke = default)
        {
            this.system = system;
            this.Wallet = wallet;
            this.ExtraGasInvoke = extraGasInvoke;
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

        private JObject GetInvokeResult(byte[] script, IVerifiable checkWitnessHashes = null)
        {
            using (ApplicationEngine engine = ApplicationEngine.Run(script, checkWitnessHashes, extraGAS: ExtraGasInvoke))
            {
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
                case RelayResultReason.PolicyFail:
                    throw new RpcException(-505, "One of the Policy filters failed.");
                default:
                    throw new RpcException(-500, "Unknown error.");
            }
        }

        private JObject Process(string method, JArray _params)
        {
            switch (method)
            {
                case "getaccountstate_old":
                    {
                        UInt160 script_hash = _params[0].AsString().ToScriptHash();
                        return GetAccountState(script_hash);
                    }
                case "getassetstate":
                    {
                        UInt256 asset_id = UInt256.Parse(_params[0].AsString());
                        return GetAssetState(asset_id);
                    }
                case "getbestblockhash":
                    {
                        return GetBestBlockHash();
                    }
                case "getblock":
                    {
                        JObject key = _params[0];
                        bool verbose = _params.Count >= 2 && _params[1].AsBoolean();
                        return GetBlock(key, verbose);
                    }
                case "getblockcount":
                    {
                        return GetBlockCount();
                    }
                case "getblockhash":
                    {
                        uint height = uint.Parse(_params[0].AsString());
                        return GetBlockHash(height);
                    }
                case "getblockheader":
                    {
                        JObject key = _params[0];
                        bool verbose = _params.Count >= 2 && _params[1].AsBoolean();
                        return GetBlockHeader(key, verbose);
                    }
                case "getblocksysfee":
                    {
                        uint height = uint.Parse(_params[0].AsString());
                        return GetBlockSysFee(height);
                    }
                case "getconnectioncount":
                    {
                        return GetConnectionCount();
                    }
                case "getcontractstate":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        return GetContractState(script_hash);
                    }
                case "getpeers":
                    {
                        return GetPeers();
                    }
                case "getrawmempool":
                    {
                        bool shouldGetUnverified = _params.Count >= 1 && _params[0].AsBoolean();
                        return GetRawMemPool(shouldGetUnverified);
                    }
                case "getrawtransaction":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        bool verbose = _params.Count >= 2 && _params[1].AsBoolean();
                        return GetRawTransaction(hash, verbose);
                    }
                case "getstorage":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        byte[] key = _params[1].AsString().HexToBytes();
                        return GetStorage(script_hash, key);
                    }
                case "gettransactionheight":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        return GetTransactionHeight(hash);
                    }
                case "gettxout":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        ushort index = ushort.Parse(_params[1].AsString());
                        return GetTxOut(hash, index);
                    }
                case "getvalidators":
                    {
                        return GetValidators();
                    }
                case "getversion":
                    {
                        return GetVersion();
                    }
                case "invoke":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        ContractParameter[] parameters = ((JArray)_params[1]).Select(p => ContractParameter.FromJson(p)).ToArray();
                        CheckWitnessHashes checkWitnessHashes = null;
                        if (_params.Count > 2)
                        {
                            UInt160[] scriptHashesForVerifying = _params.Skip(2).Select(u => UInt160.Parse(u.AsString())).ToArray();
                            checkWitnessHashes = new CheckWitnessHashes(scriptHashesForVerifying);
                        }
                        return Invoke(script_hash, parameters, checkWitnessHashes);
                    }
                case "invokefunction":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        string operation = _params[1].AsString();
                        ContractParameter[] args = _params.Count >= 3 ? ((JArray)_params[2]).Select(p => ContractParameter.FromJson(p)).ToArray() : new ContractParameter[0];
                        CheckWitnessHashes checkWitnessHashes = null;
                        if (_params.Count > 3)
                        {
                            UInt160[] scriptHashesForVerifying = _params.Skip(3).Select(u => UInt160.Parse(u.AsString())).ToArray();
                            checkWitnessHashes = new CheckWitnessHashes(scriptHashesForVerifying);
                        }
                        return InvokeFunction(script_hash, operation, args, checkWitnessHashes);
                    }
                case "invokescript":
                    {
                        byte[] script = _params[0].AsString().HexToBytes();
                        CheckWitnessHashes checkWitnessHashes = null;
                        if (_params.Count > 1)
                        {
                            UInt160[] scriptHashesForVerifying = _params.Skip(1).Select(u => UInt160.Parse(u.AsString())).ToArray();
                            checkWitnessHashes = new CheckWitnessHashes(scriptHashesForVerifying);
                        }
                        return InvokeScript(script, checkWitnessHashes);
                    }
                case "listplugins":
                    {
                        return ListPlugins();
                    }
                case "sendrawtransaction":
                    {
                        Transaction tx = Transaction.DeserializeFrom(_params[0].AsString().HexToBytes());
                        return SendRawTransaction(tx);
                    }
                case "submitblock":
                    {
                        Block block = _params[0].AsString().HexToBytes().AsSerializable<Block>();
                        return SubmitBlock(block);
                    }
                case "validateaddress":
                    {
                        string address = _params[0].AsString();
                        return ValidateAddress(address);
                    }
                case "getstateheight":
                    {
                        return GetStateHeight();
                    }
                case "getstateroot":
                    {
                        JObject param = _params[0];
                        return GetStateRoot(param);
                    }
                case "getproof":
                    {
                        UInt256 state_root = UInt256.Parse(_params[0].AsString());
                        UInt160 script_hash = UInt160.Parse(_params[1].AsString());
                        byte[] store_key = _params[2].AsString().HexToBytes();
                        return GetStateProof(state_root, script_hash, store_key);
                    }
                case "verifyproof":
                    {
                        UInt256 state_root = UInt256.Parse(_params[0].AsString());
                        byte[] proof_bytes = _params[1].AsString().HexToBytes();
                        return VerifyProof(state_root, proof_bytes);
                    }
                case "getminimumnetworkfee":
                    {
                        return GetMinimumNetworkFee();
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
                    plugin.PreProcess(context, method, _params);
                foreach (IRpcPlugin plugin in Plugin.RpcPlugins)
                {
                    result = plugin.OnProcess(context, method, _params);
                    if (result != null) break;
                }
                if (result == null)
                    result = Process(method, _params);
                foreach (IRpcPlugin plugin in Plugin.RpcPlugins)
                    plugin.PostProcess(context, method, _params, result);
            }
            catch (FormatException)
            {
                return CreateErrorResponse(request["id"], -32602, "Invalid params");
            }
            catch (IndexOutOfRangeException)
            {
                return CreateErrorResponse(request["id"], -32602, "Invalid params");
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

        public void Start(IPAddress bindAddress, int port, string sslCert = null, string password = null, string[] trustedAuthorities = null, int maxConcurrentConnections = 40)
        {
            host = new WebHostBuilder().UseKestrel(options => options.Listen(bindAddress, port, listenOptions =>
            {
                // Default value is unlimited
                if (maxConcurrentConnections == 0)
                {
                    options.Limits.MaxConcurrentConnections = 40;
                }
                else
                {
                    options.Limits.MaxConcurrentConnections = maxConcurrentConnections;
                }
                // Default value is 2 minutes
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(1);
                // Default value is 30 seconds
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);

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

        private JObject GetAccountState(UInt160 script_hash)
        {
            AccountState account = Blockchain.Singleton.Store.GetAccounts().TryGet(script_hash) ?? new AccountState(script_hash);
            return account.ToJson();
        }

        private JObject GetAssetState(UInt256 asset_id)
        {
            AssetState asset = Blockchain.Singleton.Store.GetAssets().TryGet(asset_id);
            return asset?.ToJson() ?? throw new RpcException(-100, "Unknown asset");
        }

        private JObject GetBestBlockHash()
        {
            return Blockchain.Singleton.CurrentBlockHash.ToString();
        }

        private JObject GetBlock(JObject key, bool verbose)
        {
            Block block;
            if (key is JNumber)
            {
                uint index = uint.Parse(key.AsString());
                block = Blockchain.Singleton.Store.GetBlock(index);
            }
            else
            {
                UInt256 hash = UInt256.Parse(key.AsString());
                block = Blockchain.Singleton.Store.GetBlock(hash);
            }
            if (block == null)
                throw new RpcException(-100, "Unknown block");
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

        private JObject GetBlockCount()
        {
            return Blockchain.Singleton.Height + 1;
        }

        private JObject GetBlockHash(uint height)
        {
            if (height <= Blockchain.Singleton.Height)
            {
                return Blockchain.Singleton.GetBlockHash(height).ToString();
            }
            throw new RpcException(-100, "Invalid Height");
        }

        private JObject GetBlockHeader(JObject key, bool verbose)
        {
            Header header;
            if (key is JNumber)
            {
                uint height = uint.Parse(key.AsString());
                header = Blockchain.Singleton.Store.GetHeader(height);
            }
            else
            {
                UInt256 hash = UInt256.Parse(key.AsString());
                header = Blockchain.Singleton.Store.GetHeader(hash);
            }
            if (header == null)
                throw new RpcException(-100, "Unknown block");

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

        private JObject GetBlockSysFee(uint height)
        {
            if (height <= Blockchain.Singleton.Height)
            {
                return Blockchain.Singleton.Store.GetSysFeeAmount(height).ToString();
            }
            throw new RpcException(-100, "Invalid Height");
        }

        private JObject GetConnectionCount()
        {
            return LocalNode.Singleton.ConnectedCount;
        }

        private JObject GetContractState(UInt160 script_hash)
        {
            ContractState contract = Blockchain.Singleton.Store.GetContracts().TryGet(script_hash);
            return contract?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
        }

        private JObject GetPeers()
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

        private JObject GetRawMemPool(bool shouldGetUnverified)
        {
            if (!shouldGetUnverified)
                return new JArray(Blockchain.Singleton.MemPool.GetVerifiedTransactions().Select(p => (JObject)p.Hash.ToString()));

            JObject json = new JObject();
            json["height"] = Blockchain.Singleton.Height;
            Blockchain.Singleton.MemPool.GetVerifiedAndUnverifiedTransactions(
                out IEnumerable<Transaction> verifiedTransactions,
                out IEnumerable<Transaction> unverifiedTransactions);
            json["verified"] = new JArray(verifiedTransactions.Select(p => (JObject)p.Hash.ToString()));
            json["unverified"] = new JArray(unverifiedTransactions.Select(p => (JObject)p.Hash.ToString()));
            return json;
        }

        private JObject GetRawTransaction(UInt256 hash, bool verbose)
        {
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

        private JObject GetStorage(UInt160 script_hash, byte[] key)
        {
            StorageItem item = Blockchain.Singleton.Store.GetStorages().TryGet(new StorageKey
            {
                ScriptHash = script_hash,
                Key = key
            }) ?? new StorageItem();
            return item.Value?.ToHexString();
        }

        private JObject GetTransactionHeight(UInt256 hash)
        {
            uint? height = Blockchain.Singleton.Store.GetTransactions().TryGet(hash)?.BlockIndex;
            if (height.HasValue) return height.Value;
            throw new RpcException(-100, "Unknown transaction");
        }

        private JObject GetTxOut(UInt256 hash, ushort index)
        {
            return Blockchain.Singleton.Store.GetUnspent(hash, index)?.ToJson(index);
        }

        private JObject GetValidators()
        {
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
        }

        private JObject GetVersion()
        {
            JObject json = new JObject();
            json["port"] = LocalNode.Singleton.ListenerPort;
            json["nonce"] = LocalNode.Nonce;
            json["useragent"] = LocalNode.UserAgent;
            return json;
        }

        private JObject Invoke(UInt160 script_hash, ContractParameter[] parameters, IVerifiable checkWitnessHashes = null)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                script = sb.EmitAppCall(script_hash, parameters).ToArray();
            }
            return GetInvokeResult(script, checkWitnessHashes);
        }

        private JObject InvokeFunction(UInt160 script_hash, string operation, ContractParameter[] args, IVerifiable checkWitnessHashes = null)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                script = sb.EmitAppCall(script_hash, operation, args).ToArray();
            }
            return GetInvokeResult(script, checkWitnessHashes);
        }

        private JObject InvokeScript(byte[] script, IVerifiable checkWitnessHashes = null)
        {
            return GetInvokeResult(script, checkWitnessHashes);
        }

        private JObject ListPlugins()
        {
            return new JArray(Plugin.Plugins
                .OrderBy(u => u.Name)
                .Select(u => new JObject
                {
                    ["name"] = u.Name,
                    ["version"] = u.Version.ToString(),
                    ["interfaces"] = new JArray(u.GetType().GetInterfaces()
                        .Select(p => p.Name)
                        .Where(p => p.EndsWith("Plugin"))
                        .Select(p => (JObject)p))
                }));
        }

        private JObject SendRawTransaction(Transaction tx)
        {
            RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(tx).Result;
            return GetRelayResult(reason);
        }

        private JObject SubmitBlock(Block block)
        {
            RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(block).Result;
            return GetRelayResult(reason);
        }

        private JObject ValidateAddress(string address)
        {
            JObject json = new JObject();
            UInt160 scriptHash;
            try
            {
                scriptHash = address.ToScriptHash();
            }
            catch
            {
                scriptHash = null;
            }
            json["address"] = address;
            json["isvalid"] = scriptHash != null;
            return json;
        }

        private JObject GetStateHeight()
        {
            var json = new JObject();
            json["blockheight"] = Blockchain.Singleton.Height;
            json["stateheight"] = Blockchain.Singleton.StateHeight;
            return json;
        }

        private JObject GetStateRoot(JObject key)
        {
            StateRootState state = null;
            if (key is JNumber)
            {
                state = Blockchain.Singleton.GetStateRoot((uint)key.AsNumber());
            }
            else if (key is JString)
            {
                var hash = UInt256.Parse(key.AsString());
                state = Blockchain.Singleton.GetStateRoot(hash);
            }
            else
            {
                throw new RpcException(-100, "Invalid parameter.");
            }
            if (state is null) throw new RpcException(-100, "Unknown state root.");
            return state.ToJson();
        }

        private JObject GetStateProof(UInt256 state_root, UInt160 script_hash, byte[] store_key)
        {
            JObject json = new JObject();
            var skey = new StorageKey
            {
                ScriptHash = script_hash,
                Key = store_key,
            };
            var result = Blockchain.Singleton.GetStateProof(state_root, skey, out HashSet<byte[]> proof);
            json["success"] = result;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.WriteVarBytes(skey.ToArray());
                writer.WriteVarInt(proof.Count);
                foreach (var item in proof)
                {
                    writer.WriteVarBytes(item);
                }
                writer.Flush();
                json["proof"] = ms.ToArray().ToHexString();
            }

            return json;
        }

        private JObject GetMinimumNetworkFee()
        {
            return ProtocolSettings.Default.MinimumNetworkFee.ToString();
        }

        private JObject VerifyProof(UInt256 state_root, byte[] proof_bytes)
        {
            var json = new JObject();
            var proof = new HashSet<byte[]>(ByteArrayEqualityComparer.Default);
            byte[] key;
            using (MemoryStream ms = new MemoryStream(proof_bytes, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                key = reader.ReadVarBytes(ExtensionNode.MaxKeyLength);
                var count = reader.ReadVarInt();
                for (ulong i = 0; i < count; i++)
                {
                    proof.Add(reader.ReadVarBytes());
                }
            }
            var result = Blockchain.Singleton.VerifyProof(state_root, key, proof, out byte[] value);
            if (!result)
            {
                json = "invalid";
            }
            else
            {
                json["value"] = value.ToHexString();
            }
            return json;
        }
    }
}
