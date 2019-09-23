using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Linq;
using System.Numerics;

namespace Neo.Network.RPC
{
    /// <summary>
    /// NEO APIs throught RPC
    /// </summary>
    public class NeoAPI
    {
        readonly RpcClient rpcClient;
        public ContractClient ContractClient { get; }
        public Nep5API Nep5API { get; }
        public PolicyAPI PolicyAPI { get; }

        /// <summary>
        /// NeoAPI Constructor
        /// </summary>
        /// <param name="rpc">the RPC client to call NEO RPC methods</param>
        public NeoAPI(RpcClient rpc)
        {
            rpcClient = rpc;
            ContractClient = new ContractClient(rpc);
            Nep5API = new Nep5API(rpc);
            PolicyAPI = new PolicyAPI(rpc);
        }

        /// <summary>
        /// Get unclaimed gas
        /// </summary>
        /// <param name="addressOrHash">account address or scripthash string</param>
        /// <returns></returns>
        public BigInteger GetUnclaimedGas(string addressOrHash)
        {
            UInt160 scriptHash = NativeContract.NEO.Hash;
            UInt160 account = addressOrHash.ToUInt160();
            return ContractClient.TestInvoke(scriptHash, "unclaimedGas", account, rpcClient.GetBlockCount() - 1)
                .Stack.Single().ToStackItem().GetBigInteger();
        }

        /// <summary>
        /// The GAS is claimed when doing NEO transfer
        /// This function will transfer NEO balance from account to itself
        /// </summary>
        /// <param name="key">account KeyPair</param>
        /// <returns>The transaction sended</returns>
        public Transaction ClaimGas(KeyPair key)
        {
            UInt160 toHash = key.ToScriptHash();
            BigInteger balance = Nep5API.BalanceOf(NativeContract.NEO.Hash, toHash);
            Transaction transaction = Nep5API.Transfer(NativeContract.NEO.Hash, key, toHash, balance);
            rpcClient.SendRawTransaction(transaction);
            return transaction;
        }

        /// <summary>
        /// Transfer NEP5 token balance
        /// </summary>
        /// <param name="tokenHash">nep5 token script hash</param>
        /// <param name="fromKey">wif or private key</param>
        /// <param name="toAddress">address or account script hash</param>
        /// <param name="amount">token amount</param>
        /// <param name="networkFee">netwotk fee, set to be 0 will auto calculate the least fee</param>
        /// <returns></returns>
        public Transaction Transfer(string tokenHash, string fromKey, string toAddress, decimal amount, long networkFee = 0)
        {
            UInt160 scriptHash = tokenHash.ToUInt160();
            var tokenInfo = Nep5API.GetTokenInfo(scriptHash);

            KeyPair from = fromKey.ToKeyPair();
            UInt160 to = toAddress.ToUInt160();
            BigInteger amountInteger = 0;
            Transaction transaction = Nep5API.Transfer(scriptHash, from, to, amountInteger, networkFee);
            rpcClient.SendRawTransaction(transaction);
            return transaction;
        }
    }
}
