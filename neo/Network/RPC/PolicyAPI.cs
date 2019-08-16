using Neo.SmartContract.Native;
using Neo.VM;
using System.Linq;

namespace Neo.Network.RPC
{
    public class PolicyAPI : ContractClient
    {
        readonly UInt160 scriptHash = NativeContract.Policy.Hash;

        public PolicyAPI(RpcClient rpcClient) : base(rpcClient) { }

        public uint GetMaxTransactionsPerBlock()
        {
            return (uint)TestInvoke(scriptHash, "getMaxTransactionsPerBlock").Stack.Single().ToStackItem().GetBigInteger();
        }

        public long GetFeePerByte()
        {
            return (long)TestInvoke(scriptHash, "getFeePerByte").Stack.Single().ToStackItem().GetBigInteger();
        }

        public UInt160[] GetBlockedAccounts()
        {
            var result = (VM.Types.Array)TestInvoke(scriptHash, "getBlockedAccounts").Stack.Single().ToStackItem();
            return result.Select(p => new UInt160(p.GetByteArray())).ToArray();
        }
    }
}
