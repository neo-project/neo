using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SDK.TX;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.Wallets;

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

        /// <summary>
        /// Use RPC method to test invoke operation.
        /// </summary>
        public RpcInvokeResult TestInvoke(UInt160 scriptHash, string operation, params object[] args)
        {
            byte[] script = MakeScript(scriptHash, operation, args);
            return rpcClient.InvokeScript(script);
        }

        /// <summary>
        /// Deploy Contract
        /// </summary>
        public bool DeployContract(byte[] contractScript, bool hasStorage, bool isPayable, KeyPair sender, long networkFee = 0)
        {
            ContractFeatures properties = ContractFeatures.NoProperty;
            if (hasStorage) properties |= ContractFeatures.HasStorage;
            if (isPayable) properties |= ContractFeatures.Payable;

            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(InteropService.Neo_Contract_Create, contractScript, properties);
                script = sb.ToArray();
            }

            Transaction tx = new TxManager(rpcClient, sender.ScriptHash)
                .MakeTransaction(script, null, null, networkFee)
                .AddSignature(sender)
                .Sign()
                .Tx;

            return rpcClient.SendRawTransaction(tx.ToArray().ToHexString());

        }
    }
}
