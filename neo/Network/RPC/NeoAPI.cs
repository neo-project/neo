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
        public decimal GetUnclaimedGas(string addressOrHash)
        {
            UInt160 scriptHash = NativeContract.NEO.Hash;
            UInt160 account = addressOrHash.ToUInt160();
            BigInteger balance = ContractClient.TestInvoke(scriptHash, "unclaimedGas", account, rpcClient.GetBlockCount() - 1)
                .Stack.Single().ToStackItem().GetBigInteger();
            return ((decimal)balance) / (long)NativeContract.GAS.Factor;
        }

        /// <summary>
        /// Get Neo Balance
        /// </summary>
        /// <param name="addressOrHash">address or hash</param>
        /// <returns></returns>
        public uint GetNeoBalance(string addressOrHash)
        {
            BigInteger balance = GetTokenBalance(NativeContract.NEO.Hash.ToString(), addressOrHash);
            return (uint)balance;
        }

        /// <summary>
        /// Get Gas Balance
        /// </summary>
        /// <param name="addressOrHash">address or hash</param>
        /// <returns></returns>
        public decimal GetGasBalance(string addressOrHash)
        {
            BigInteger balance = GetTokenBalance(NativeContract.GAS.Hash.ToString(), addressOrHash);
            return ((decimal)balance) / (long)NativeContract.GAS.Factor;
        }

        /// <summary>
        /// Get token balance
        /// </summary>
        /// <param name="tokenHash">token script hash</param>
        /// <param name="addressOrHash">address or hash</param>
        /// <returns></returns>
        public BigInteger GetTokenBalance(string tokenHash, string addressOrHash)
        {
            UInt160 scriptHash = tokenHash.ToUInt160();
            UInt160 account = addressOrHash.ToUInt160();
            return Nep5API.BalanceOf(scriptHash, account);
        }

        /// <summary>
        /// The GAS is claimed when doing NEO transfer
        /// This function will transfer NEO balance from account to itself
        /// </summary>
        /// <param name="key">wif or private key</param>
        /// <returns>The transaction sended</returns>
        public Transaction ClaimGas(string key)
        {
            KeyPair keyPair = key.ToKeyPair();
            UInt160 toHash = keyPair.ToScriptHash();
            BigInteger balance = Nep5API.BalanceOf(NativeContract.NEO.Hash, toHash);
            Transaction transaction = Nep5API.GetTransfer(NativeContract.NEO.Hash, keyPair, toHash, balance);
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
        /// Wait until the transaction is observable block chain
        /// </summary>
        /// <param name="transaction">the transaction to observe</param>
        /// <param name="timeout">TimeoutException throws after "timeout" seconds</param>
        /// <returns>the Transaction Height</returns>
        public async Task<uint> WaitTransaction(Transaction transaction, int timeout = 60)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeout);
            uint current = 0;
            while (current == 0)
            {
                if (deadline < DateTime.UtcNow)
                {
                    throw new TimeoutException();
                }

                try
                {
                    current = rpcClient.GetTransactionHeight(transaction.Hash.ToString());
                    if (current == 0)
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception) { }
            }
            return current;
        }
    }
}
