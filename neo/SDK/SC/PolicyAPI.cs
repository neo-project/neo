using Neo.Network.RPC;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Linq;

namespace Neo.SDK.SC
{
    public class PolicyAPI : ContractClient
    {
        readonly UInt160 scriptHash = NativeContract.Policy.Hash;

        public PolicyAPI(RpcClient rpcClient) : base(rpcClient) { }

        public long GetFeePerByte()
        {
            return (long)TestInvoke(scriptHash, "getFeePerByte").Stack.Single().ToStackItem().GetBigInteger();
        }
    }
}
