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
    public class RpcClient : IRpcClient, IDisposable
    {
        private readonly HttpService rpcHelper;

        public RpcClient(HttpService rpc)
        {
            rpcHelper = rpc;
        }

        public void Dispose()
        {
            rpcHelper?.Dispose();
        }

        private JObject RpcSend(string method, params JObject[] paraArgs)
        {
            var request = new RpcRequest
            {
                Id = 1,
                Jsonrpc = "2.0",
                Method = method,
                Params = paraArgs.Select(p => p).ToArray()
            };
            return rpcHelper.Send(request).Result;
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

        public RpcBlock GetBlock(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcBlock.FromJson(RpcSend("getblock", index, true));
            }
            return RpcBlock.FromJson(RpcSend("getblock", hashOrIndex, true));
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

        public RpcBlockHeader GetBlockHeader(string hashOrIndex)
        {
            if (int.TryParse(hashOrIndex, out int index))
            {
                return RpcBlockHeader.FromJson(RpcSend("getblockheader", index, true));
            }
            return RpcBlockHeader.FromJson(RpcSend("getblockheader", hashOrIndex, true));
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

        public RpcPeers GetPeers()
        {
            return RpcPeers.FromJson(RpcSend("getpeers"));
        }

        public string[] GetRawMempool()
        {
            return ((JArray)RpcSend("getrawmempool")).Select(p => p.AsString()).ToArray();
        }

        public RpcRawMemPool GetRawMempoolBoth()
        {
            return RpcRawMemPool.FromJson(RpcSend("getrawmempool"));
        }

        public string GetRawTransactionHex(string txid)
        {
            return RpcSend("getrawtransaction", txid).AsString();
        }

        public RpcTransaction GetRawTransaction(string txid)
        {
            return RpcTransaction.FromJson(RpcSend("getrawtransaction", txid, true));
        }

        public string GetStorage(string script_hash, string key)
        {
            return RpcSend("getstorage", script_hash, key).AsString();
        }

        public uint GetTransactionHeight(string txid)
        {
            return uint.Parse(RpcSend("gettransactionheight", txid).AsString());
        }

        public RpcValidator[] GetValidators()
        {
            return ((JArray)RpcSend("getvalidators")).Select(p => RpcValidator.FromJson(p)).ToArray();
        }

        public RpcVersion GetVersion()
        {
            return RpcVersion.FromJson(RpcSend("getversion"));
        }

        public RpcInvokeResult InvokeFunction(string address, string function, RpcStack[] stacks)
        {
            return RpcInvokeResult.FromJson(RpcSend("invokefunction", address, function, stacks.Select(p => p.ToJson()).ToArray()));
        }

        public RpcInvokeResult InvokeScript(string script)
        {
            return RpcInvokeResult.FromJson(RpcSend("invokescript", script));
        }

        public RpcPlugin[] ListPlugins()
        {
            return ((JArray)RpcSend("listplugins")).Select(p => RpcPlugin.FromJson(p)).ToArray();
        }

        public bool SendRawTransaction(string rawTransaction)
        {
            return RpcSend("sendrawtransaction", rawTransaction).AsBoolean();
        }

        public bool SubmitBlock(string block)
        {
            return RpcSend("submitblock", block).AsBoolean();
        }

        public RpcValidateAddressResult ValidateAddress(string address)
        {
            return RpcValidateAddressResult.FromJson(RpcSend("validateaddress", address));
        }

    }
}
