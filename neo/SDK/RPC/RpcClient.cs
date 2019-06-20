using System;
using System.Collections.Generic;
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

        private T RpcSend<T>(string method, params object[] paraArgs)
        {
            var request = new RPCRequest
            {
                Id = 1,
                Jsonrpc = "2.0",
                Method = method,
                Params = paraArgs
            };
            return rpcHelper.Send<T>(request);
        }

        public string GetBestBlockHash()
        {
            return RpcSend<string>("getbestblockhash");
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

        public TxJson GetRawTransaction(string txid)
        {
            return RpcSend<TxJson>("getrawtransaction", txid);
        }

        public string GetStorage(string script_hash, string key)
        {
            return RpcSend<string>("getstorage", script_hash, key);
        }

        public uint GetTransactionHeight(string txid)
        {
            return RpcSend<uint>("gettransactionheight", txid);
        }

        public Validator[] GetValidators()
        {
            return RpcSend<Validator[]>("getvalidators");
        }

        public GetVersion GetVersion()
        {
            return RpcSend<GetVersion>("getversion");
        }

        public InvokeRet InvokeFunction(string address, string function, Stack[] stacks)
        {
            return RpcSend<InvokeRet>("invokefunction", address, function, stacks);
        }

        public InvokeRet InvokeScript(string script)
        {
            return RpcSend<InvokeRet>("invokescript", script);
        }

        public List<Plugin> ListPlugins()
        {
            return RpcSend<List<Plugin>>("listplugins");
        }

        public bool SendRawTransaction(string rawTransaction)
        {
            return RpcSend<bool>("sendrawtransaction", rawTransaction);
        }

        public bool SubmitBlock(string block)
        {
            return RpcSend<bool>("submitblock", block);
        }

        public ValidateAddress ValidateAddress(string address)
        {
            return RpcSend<ValidateAddress>("validateaddress");
        }

        // wait for plugin for neo3.
        public GetNep5Balances GetNep5Balances(string address)
        {
            throw new NotImplementedException();
            //turn RpcSend<GetNep5Balances>("getnep5balances", address);
        }

    }
}
