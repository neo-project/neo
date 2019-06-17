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

        public GetAccountState GetAccountState(string address)
        {
            return RpcSend<GetAccountState>("getaccountstate", address);
        }

        public GetClaimable GetClaimable(string address)
        {
            return RpcSend<GetClaimable>("getclaimable", address);
        }

        public GetNep5Balances GetNep5Balances(string address)
        {
            return RpcSend<GetNep5Balances>("getnep5balances", address);
        }

        public GetUnspents GetUnspents(string address)
        {
            return RpcSend<GetUnspents>("getunspents", address);
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
    }
}
