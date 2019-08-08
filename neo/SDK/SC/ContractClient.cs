using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.VM;

namespace Neo.SDK.SC
{
    public class ContractClient
    {
        protected readonly RpcClient rpcClient;

        public ContractClient(RpcClient rpc)
        {
            rpcClient = rpc;
        }

        /// <summary>
        /// Use RPC method to test invoke operation.
        /// </summary>
        public RpcInvokeResult TestInvoke(UInt160 scriptHash, string operation, params object[] args)
        {
            byte[] script = MakeScript(scriptHash, operation, args);
            return rpcClient.InvokeScript(script);
        }

        /// <summary>
        /// Generate scripts to call a specific method from a specific contract.
        /// </summary>
        public static byte[] MakeScript(UInt160 scriptHash, string operation, params object[] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                if (args.Length > 0)
                    sb.EmitAppCall(scriptHash, operation, args);
                else
                    sb.EmitAppCall(scriptHash, operation);
                return sb.ToArray();
            }
        }
    }
}
