using AntShares.Core;
using AntShares.IO.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace AntShares.Network.RPC
{
    internal class RpcServer : IDisposable
    {
#if TESTNET
        public const int DEFAULT_PORT = 20332;
#else
        public const int DEFAULT_PORT = 10332;
#endif

        private HttpListener listener = new HttpListener();
        private bool stopped = false;

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
            if (listener.IsListening)
            {
                listener.Stop();
                while (!stopped) Thread.Sleep(100);
            }
            listener.Close();
        }

        private static JObject InternalCall(string method, JArray _params)
        {
            switch (method)
            {
                case "getbestblockhash":
                    return Blockchain.Default.CurrentBlockHash.ToString();
                case "getblock":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        Block block = Blockchain.Default.GetBlock(hash);
                        if (block == null)
                            throw new RpcException(-100, "Unknown block");
                        return block.ToJson();
                    }
                case "getblockcount":
                    return Blockchain.Default.Height + 1;
                case "getblockhash":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        return Blockchain.Default.GetBlockHash(height).ToString();
                    }
                case "getrawtransaction":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        Transaction tx = Blockchain.Default.GetTransaction(hash);
                        if (tx == null)
                            throw new RpcException(-101, "Unknown transaction");
                        return tx.ToJson();
                    }
                default:
                    throw new RpcException(-32601, "Method not found");
            }
        }

        private static void Process(HttpListenerContext context)
        {
            try
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
                context.Response.AddHeader("Access-Control-Allow-Methods", "POST");
                context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                context.Response.AddHeader("Access-Control-Max-Age", "31536000");
                if (context.Request.HttpMethod != "POST") return;
                JObject request = null;
                JObject response;
                using (StreamReader reader = new StreamReader(context.Request.InputStream))
                {
                    try
                    {
                        request = JObject.Parse(reader);
                    }
                    catch (FormatException) { }
                }
                if (request == null)
                {
                    response = CreateErrorResponse(null, -32700, "Parse error");
                }
                else if (request is JArray)
                {
                    JArray array = (JArray)request;
                    if (array.Count == 0)
                    {
                        response = CreateErrorResponse(request["id"], -32600, "Invalid Request");
                    }
                    else
                    {
                        response = array.Select(p => ProcessRequest(p)).Where(p => p != null).ToArray();
                    }
                }
                else
                {
                    response = ProcessRequest(request);
                }
                if (response == null || (response as JArray)?.Count == 0) return;
                context.Response.ContentType = "application/json-rpc";
                using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
                {
                    writer.Write(response.ToString());
                }
            }
            finally
            {
                context.Response.Close();
            }
        }

        private static JObject ProcessRequest(JObject request)
        {
            if (!request.ContainsProperty("id")) return null;
            if (!request.ContainsProperty("method") || !request.ContainsProperty("params") || !(request["params"] is JArray))
            {
                return CreateErrorResponse(request["id"], -32600, "Invalid Request");
            }
            JObject result = null;
            try
            {
                result = InternalCall(request["method"].AsString(), (JArray)request["params"]);
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

        public async void Start(string host = "*", int port = DEFAULT_PORT)
        {
            listener.Prefixes.Add($"http://{host}:{port}/");
            listener.Start();
            while (listener.IsListening)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (HttpListenerException) { }
                if (context != null) Process(context);
            }
            stopped = true;
        }
    }
}
