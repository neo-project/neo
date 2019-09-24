using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

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
        /// <param name="addressOrHash">account address, scripthash or public key string</param>
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
            Transaction transaction = Nep5API.GetTransfer(NativeContract.NEO.Hash, key, toHash, balance);
            rpcClient.SendRawTransaction(transaction);
            return transaction;
        }

        /// <summary>
        /// Transfer NEP5 token balance, with common data types
        /// </summary>
        /// <param name="tokenHash">nep5 token script hash</param>
        /// <param name="fromKey">wif or private key</param>
        /// <param name="toAddress">address or account script hash</param>
        /// <param name="amount">token amount</param>
        /// <param name="networkFee">netwotk fee, set to be 0 will auto calculate the least fee</param>
        /// <returns></returns>
        public Transaction Transfer(string tokenHash, string fromKey, string toAddress, decimal amount, decimal networkFee = 0)
        {
            UInt160 scriptHash = tokenHash.ToUInt160();
            var decimals = Nep5API.Decimals(scriptHash);

            KeyPair from = fromKey.ToKeyPair();
            UInt160 to = toAddress.ToUInt160();
            BigInteger amountInteger = amount.ToBigInteger(decimals);
            BigInteger networkFeeInteger = networkFee.ToBigInteger(NativeContract.GAS.Decimals);
            Transaction transaction = Nep5API.GetTransfer(scriptHash, from, to, amountInteger, (long)networkFeeInteger);
            rpcClient.SendRawTransaction(transaction);
            return transaction;
        }

        /// <summary>
        /// Wait until New Block comes
        /// Won't block sync code if don't call Task.Wait()
        /// </summary>
        /// <returns>new block index</returns>
        public async Task<uint> WaitNewBlock()
        {
            uint start = rpcClient.GetBlockCount() - 1;
            DateTime deadline = DateTime.UtcNow.AddSeconds(31);
            uint current = start;
            while (start == current)
            {
                if (deadline < DateTime.UtcNow)
                {
                    throw new TimeoutException();
                }

                await Task.Delay(1000);
                current = rpcClient.GetBlockCount() - 1;
            }

            return current;
        }
    }
}
