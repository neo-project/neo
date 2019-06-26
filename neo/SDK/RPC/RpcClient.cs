using System;
using System.Collections.Generic;
using System.Linq;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.SDK.RPC;
using Neo.SDK.RPC.Model;

namespace Neo.SDK
{
    public class RpcClient : IRpcClient
    {
        private readonly IRpcService rpcHelper;

        public RpcClient(IRpcService rpc)
        {
            rpcHelper = rpc;
        }

        private JObject RpcSend(string method, params object[] paraArgs)
        {
            var request = new RPCRequest
            {
                Id = 1,
                Jsonrpc = "2.0",
                Method = method,
                Params = paraArgs.Select(p => (JObject)p).ToArray()
            };
            return rpcHelper.Send(request);
        }

        private T RpcSend<T>(string method, params object[] paraArgs)
        {
            throw new NotImplementedException();
        }

        public string GetBestBlockHash()
        {
            return RpcSend("getbestblockhash").AsString();
        }

        public string GetBlockHex(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend<string>("getblock", index);
            }
            return RpcSend<string>("getblock", hashOrIndex);
        }

        public GetBlock GetBlock(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend<GetBlock>("getblock", index, true);
            }
            return RpcSend<GetBlock>("getblock", hashOrIndex, true);
        }

        public int GetBlockCount()
        {
            return RpcSend<int>("getblockcount");
        }

        public string GetBlockHash(int index)
        {
            return RpcSend<string>("getblockhash", index);
        }

        public string GetBlockHeaderHex(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend<string>("getblockheader", index);
            }
            return RpcSend<string>("getblockheader", hashOrIndex);
        }

        public GetBlockHeader GetBlockHeader(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend<GetBlockHeader>("getblockheader", index, true);
            }
            return RpcSend<GetBlockHeader>("getblockheader", hashOrIndex, true);
        }

        public string GetBlockSysFee(int height)
        {
            return RpcSend<string>("getblocksysfee", height);
        }

        public int GetConnectionCount()
        {
            return RpcSend<int>("getconnectioncount");
        }

        public GetContractState GetContractState(string hash)
        {
            return RpcSend<GetContractState>("getcontractstate", hash);
        }

        public GetPeers GetPeers()
        {
            return RpcSend<GetPeers>("getpeers");
        }

        public string[] GetRawMempool()
        {
            return RpcSend<string[]>("getrawmempool");
        }

        public GetRawMempool GetRawMempoolBoth()
        {
            return RpcSend<GetRawMempool>("getrawmempool");
        }

        public string GetRawTransactionHex(string txid)
        {

            return RpcSend<string>("getrawtransaction", txid);
        }

        public Transaction GetRawTransaction(string txid)
        {
            var json = RpcSend("getrawtransaction", txid);
            return Transaction.FromJson(json["result"]);
        }

        public string GetStorage(string script_hash, string key)
        {
            var json = RpcSend("getstorage", script_hash, key);
            return json["result"].AsString();
        }

        public uint GetTransactionHeight(string txid)
        {
            var json = RpcSend("gettransactionheight", txid);
            return uint.Parse(json["result"].AsString());
        }

        public SDK_Validator[] GetValidators()
        {
            var json = RpcSend("getvalidators");
            return ((JArray)json["result"]).Select(p => SDK_Validator.FromJson(p)).ToArray();
        }

        public SDK_Version GetVersion()
        {
            var json = RpcSend("getversion");
            return SDK_Version.FromJson(json["result"]);
        }

        public SDK_InvokeScriptResult InvokeFunction(string address, string function, SDK_StackJson[] stacks)
        {
            var json = RpcSend("invokefunction", address, function, stacks);
            return SDK_InvokeScriptResult.FromJson(json["result"]);
        }

        public SDK_InvokeScriptResult InvokeScript(string script)
        {
            var json = RpcSend("invokescript", script);
            return SDK_InvokeScriptResult.FromJson(json["result"]);
        }

        public SDK_Plugin ListPlugins()
        {
            var json = RpcSend("listplugins");
            return SDK_Plugin.FromJson(json["result"]);
        }

        public bool SendRawTransaction(string rawTransaction)
        {
            var json = RpcSend("sendrawtransaction", rawTransaction);
            return json["result"].AsBoolean();
        }

        public bool SubmitBlock(string block)
        {
            var json = RpcSend("submitblock", block);
            return json["result"].AsBoolean();
        }

        public SDK_ValidateAddress ValidateAddress(string address)
        {
            var json = RpcSend("validateaddress", address);
            return SDK_ValidateAddress.FromJson(json["result"]);
        }

        public SDK_Nep5Balances GetNep5Balances(string address)
        {
            var json = RpcSend("getnep5balances", address);
            return SDK_Nep5Balances.FromJson(json["result"]);
        }

    }
}
