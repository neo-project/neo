using System;
using System.Collections.Generic;
using System.Linq;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.SDK.RPC;
using Neo.SDK.RPC.Model;

namespace Neo.SDK
{
    public class RpcClient : IRpcClient
    {
        private readonly HttpService rpcHelper;

        public RpcClient(HttpService rpc)
        {
            rpcHelper = rpc;
        }

        private JObject RpcSend(string method, params JObject[] paraArgs)
        {
            var request = new RPCRequest
            {
                Id = 1,
                Jsonrpc = "2.0",
                Method = method,
                Params = paraArgs.Select(p => p).ToArray()
            };
            return rpcHelper.Send(request);
        }

        public string GetBestBlockHash()
        {
            return RpcSend("getbestblockhash").AsString();
        }

        public string GetBlockHex(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend("getblock", index).AsString();
            }
            return RpcSend("getblock", hashOrIndex).AsString();
        }

        public SDK_Block GetBlock(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return SDK_Block.FromJson(RpcSend("getblock", index, true));
            }
            return SDK_Block.FromJson(RpcSend("getblock", hashOrIndex, true));
        }

        public int GetBlockCount()
        {
            return (int)RpcSend("getblockcount").AsNumber();
        }

        public string GetBlockHash(int index)
        {
            return RpcSend("getblockhash", index).AsString();
        }

        public string GetBlockHeaderHex(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcSend("getblockheader", index).AsString();
            }
            return RpcSend("getblockheader", hashOrIndex).AsString();
        }

        public SDK_BlockHeader GetBlockHeader(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return SDK_BlockHeader.FromJson(RpcSend("getblockheader", index, true));
            }
            return SDK_BlockHeader.FromJson(RpcSend("getblockheader", hashOrIndex, true));
        }

        public string GetBlockSysFee(int height)
        {
            return RpcSend("getblocksysfee", height).AsString();
        }

        public int GetConnectionCount()
        {
            return (int)RpcSend("getconnectioncount").AsNumber();
        }

        public ContractState GetContractState(string hash)
        {
            return ContractState.FromJson(RpcSend("getcontractstate", hash));
        }

        public SDK_GetPeersResult GetPeers()
        {
            return SDK_GetPeersResult.FromJson(RpcSend("getpeers"));
        }

        public string[] GetRawMempool()
        {
            return ((JArray)RpcSend("getrawmempool")).Select(p => p.AsString()).ToArray();
        }

        public SDK_RawMemPool GetRawMempoolBoth()
        {
            return SDK_RawMemPool.FromJson(RpcSend("getrawmempool"));
        }

        public string GetRawTransactionHex(string txid)
        {
            return RpcSend("getrawtransaction", txid).AsString();
        }

        public SDK_Transaction GetRawTransaction(string txid)
        {
            // verbose = true;
            return SDK_Transaction.FromJson(RpcSend("getrawtransaction", txid, true));
        }

        public string GetStorage(string script_hash, string key)
        {
            return RpcSend("getstorage", script_hash, key).AsString();
        }

        public uint GetTransactionHeight(string txid)
        {
            return uint.Parse(RpcSend("gettransactionheight", txid).AsString());
        }

        public SDK_Validator[] GetValidators()
        {
            return ((JArray)RpcSend("getvalidators")).Select(p => SDK_Validator.FromJson(p)).ToArray();
        }

        public SDK_Version GetVersion()
        {
            return SDK_Version.FromJson(RpcSend("getversion"));
        }

        public SDK_InvokeScriptResult InvokeFunction(string address, string function, SDK_StackJson[] stacks)
        {
            return SDK_InvokeScriptResult.FromJson(RpcSend("invokefunction", address, function, stacks.Select(p => p.ToJson()).ToArray()));
        }

        public SDK_InvokeScriptResult InvokeScript(string script)
        {
            return SDK_InvokeScriptResult.FromJson(RpcSend("invokescript", script));
        }

        public SDK_Plugin[] ListPlugins()
        {
            return ((JArray)RpcSend("listplugins")).Select(p => SDK_Plugin.FromJson(p)).ToArray();
        }

        public bool SendRawTransaction(string rawTransaction)
        {
            return RpcSend("sendrawtransaction", rawTransaction).AsBoolean();
        }

        public bool SubmitBlock(string block)
        {
            return RpcSend("submitblock", block).AsBoolean();
        }

        public SDK_ValidateAddressResult ValidateAddress(string address)
        {
            return SDK_ValidateAddressResult.FromJson(RpcSend("validateaddress", address));
        }

        public SDK_Nep5Balances GetNep5Balances(string address)
        {
            return SDK_Nep5Balances.FromJson(RpcSend("getnep5balances", address));
        }

    }
}
