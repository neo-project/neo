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

        public GetNep5Balances GetNep5Balances(string address)
        {
            return RpcSend<GetNep5Balances>("getnep5balances", address);
        }

        public InvokeRet Invoke(string address, Stack[] stacks)
        {
            return RpcSend<InvokeRet>("invoke", address, stacks);
        }

        public InvokeRet InvokeFunction(string address, string function, Stack[] stacks)
        {
            return RpcSend<InvokeRet>("invokefunction", address, function, stacks);
        }

        public InvokeRet InvokeScript(string script)
        {
            return RpcSend<InvokeRet>("invokescript", script);
        }

        public bool SendRawTransaction(string rawTransaction)
        {
            return RpcSend<bool>("sendrawtransaction", rawTransaction);
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
            throw new System.NotImplementedException();
        }

        public string GetBlockHash(int index)
        {
            throw new System.NotImplementedException();
        }

        public string GetBlockHeaderHex(string hashOrIndex)
        {
            throw new System.NotImplementedException();
        }

        public GetBlockHeader GetBlockHeader(string hashOrIndex)
        {
            throw new System.NotImplementedException();
        }

        public string GetBlockSysFee(int height)
        {
            throw new System.NotImplementedException();
        }

        public int GetConnectionCount()
        {
            throw new System.NotImplementedException();
        }

        public GetContractState GetContractState(string hash)
        {
            throw new System.NotImplementedException();
        }

        public GetPeers GetPeers()
        {
            throw new System.NotImplementedException();
        }

        public string[] GetRawMempool()
        {
            throw new System.NotImplementedException();
        }

        public GetRawMempool[] GetRawMempoolBoth()
        {
            throw new System.NotImplementedException();
        }

        public string GetRawTransactionHex(string txid)
        {
            throw new System.NotImplementedException();
        }

        public TxJson[] GetRawTransaction(string txid)
        {
            throw new System.NotImplementedException();
        }

        public string GetStorage(string script_hash, string key)
        {
            throw new System.NotImplementedException();
        }

        public uint GetTransactionHeight(string txid)
        {
            throw new System.NotImplementedException();
        }

        public GetValidator[] GetValidators()
        {
            throw new System.NotImplementedException();
        }

        public GetVersion GetVersion()
        {
            throw new System.NotImplementedException();
        }

        public List<Plugin> ListPlugins()
        {
            throw new System.NotImplementedException();
        }

        public bool SubmitBlock(string block)
        {
            throw new System.NotImplementedException();
        }

        public ValidateAddress ValidateAddress(string address)
        {
            throw new System.NotImplementedException();
        }
    }
}
