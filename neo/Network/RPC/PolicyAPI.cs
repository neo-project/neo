using Neo.SmartContract.Native;
using Neo.VM;
using System.Linq;

namespace Neo.Network.RPC
{
    /// <summary>
    /// Get Policy info by RPC API
    /// </summary>
    public class PolicyAPI : ContractClient
    {
        readonly UInt160 scriptHash = NativeContract.Policy.Hash;

        /// <summary>
        /// PolicyAPI Constructor
        /// </summary>
        /// <param name="rpcClient">the RPC client to call NEO RPC methods</param>
        public PolicyAPI(RpcClient rpcClient) : base(rpcClient) { }

        /// <summary>
        /// Get Max Transactions Count Per Block
        /// </summary>
        /// <returns></returns>
        public uint GetMaxTransactionsPerBlock()
        {
            return (uint)TestInvoke(scriptHash, "getMaxTransactionsPerBlock").Stack.Single().ToStackItem().GetBigInteger();
        }

        /// <summary>
        /// Get Max Block Size
        /// </summary>
        /// <returns></returns>
        public uint GetMaxBlockSize()
        {
            return (uint)TestInvoke(scriptHash, "getMaxBlockSize").Stack.Single().ToStackItem().GetBigInteger();
        }

        /// <summary>
        /// Get Network Fee Per Byte
        /// </summary>
        /// <returns></returns>
        public long GetFeePerByte()
        {
            return (long)TestInvoke(scriptHash, "getFeePerByte").Stack.Single().ToStackItem().GetBigInteger();
        }

        /// <summary>
        /// Get Ploicy Blocked Accounts
        /// </summary>
        /// <returns></returns>
        public UInt160[] GetBlockedAccounts()
        {
            var result = (VM.Types.Array)TestInvoke(scriptHash, "getBlockedAccounts").Stack.Single().ToStackItem();
            return result.Select(p => new UInt160(p.GetByteArray())).ToArray();
        }
    }
}
