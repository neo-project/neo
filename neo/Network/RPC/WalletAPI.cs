using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
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
    /// Wallet Common APIs
    /// </summary>
    public class WalletAPI
    {
        private readonly RpcClient rpcClient;
        private readonly Nep5API nep5API;

        /// <summary>
        /// WalletAPI Constructor
        /// </summary>
        /// <param name="rpc">the RPC client to call NEO RPC methods</param>
        public WalletAPI(RpcClient rpc)
        {
            rpcClient = rpc;
            nep5API = new Nep5API(rpc);
        }

        /// <summary>
        /// Get unclaimed gas with address, scripthash or public key string
        /// </summary>
        /// <param name="account">address, scripthash or public key string
        /// Example: address ("AV556nYUwyJKNv8Xy7hVMLQnkmKPukw6x5"), scripthash ("0x6a38cd693b615aea24dd00de12a9f5836844da91"), public key ("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575")</param>
        /// <returns></returns>
        public decimal GetUnclaimedGas(string account)
        {
            UInt160 accountHash = Utility.GetScriptHash(account);
            return GetUnclaimedGas(accountHash);
        }

        /// <summary>
        /// Get unclaimed gas
        /// </summary>
        /// <param name="account">account scripthash</param>
        /// <returns></returns>
        public decimal GetUnclaimedGas(UInt160 account)
        {
            UInt160 scriptHash = NativeContract.NEO.Hash;
            BigInteger balance = nep5API.TestInvoke(scriptHash, "unclaimedGas", account, rpcClient.GetBlockCount() - 1)
                .Stack.Single().ToStackItem().GetBigInteger();
            return ((decimal)balance) / (long)NativeContract.GAS.Factor;
        }

        /// <summary>
        /// Get Neo Balance
        /// </summary>
        /// <param name="account">address, scripthash or public key string
        /// Example: address ("AV556nYUwyJKNv8Xy7hVMLQnkmKPukw6x5"), scripthash ("0x6a38cd693b615aea24dd00de12a9f5836844da91"), public key ("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575")</param>
        /// <returns></returns>
        public uint GetNeoBalance(string account)
        {
            BigInteger balance = GetTokenBalance(NativeContract.NEO.Hash.ToString(), account);
            return (uint)balance;
        }

        /// <summary>
        /// Get Gas Balance
        /// </summary>
        /// <param name="account">address, scripthash or public key string
        /// Example: address ("AV556nYUwyJKNv8Xy7hVMLQnkmKPukw6x5"), scripthash ("0x6a38cd693b615aea24dd00de12a9f5836844da91"), public key ("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575")</param>
        /// <returns></returns>
        public decimal GetGasBalance(string account)
        {
            BigInteger balance = GetTokenBalance(NativeContract.GAS.Hash.ToString(), account);
            return ((decimal)balance) / (long)NativeContract.GAS.Factor;
        }

        /// <summary>
        /// Get token balance with string parameters
        /// </summary>
        /// <param name="tokenHash">token script hash, Example: "0x43cf98eddbe047e198a3e5d57006311442a0ca15"(NEO)</param>
        /// <param name="account">address, scripthash or public key string
        /// Example: address ("AV556nYUwyJKNv8Xy7hVMLQnkmKPukw6x5"), scripthash ("0x6a38cd693b615aea24dd00de12a9f5836844da91"), public key ("02f9ec1fd0a98796cf75b586772a4ddd41a0af07a1dbdf86a7238f74fb72503575")</param>
        /// <returns></returns>
        public BigInteger GetTokenBalance(string tokenHash, string account)
        {
            UInt160 scriptHash = Utility.GetScriptHash(tokenHash);
            UInt160 accountHash = Utility.GetScriptHash(account);
            return nep5API.BalanceOf(scriptHash, accountHash);
        }

        /// <summary>
        /// The GAS is claimed when doing NEO transfer
        /// This function will transfer NEO balance from account to itself
        /// </summary>
        /// <param name="key">wif or private key
        /// Example: WIF ("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"), PrivateKey ("450d6c2a04b5b470339a745427bae6828400cf048400837d73c415063835e005")</param>
        /// <returns>The transaction sended</returns>
        public Transaction ClaimGas(string key)
        {
            KeyPair keyPair = Utility.GetKeyPair(key);
            return ClaimGas(keyPair);
        }

        /// <summary>
        /// The GAS is claimed when doing NEO transfer
        /// This function will transfer NEO balance from account to itself
        /// </summary>
        /// <param name="keyPair">keyPair</param>
        /// <returns>The transaction sended</returns>
        public Transaction ClaimGas(KeyPair keyPair)
        {
            UInt160 toHash = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();
            BigInteger balance = nep5API.BalanceOf(NativeContract.NEO.Hash, toHash);
            Transaction transaction = nep5API.CreateTransferTx(NativeContract.NEO.Hash, keyPair, toHash, balance);
            rpcClient.SendRawTransaction(transaction);
            return transaction;
        }

        /// <summary>
        /// Transfer NEP5 token balance, with common data types
        /// </summary>
        /// <param name="tokenHash">nep5 token script hash, Example: scripthash ("0x6a38cd693b615aea24dd00de12a9f5836844da91")</param>
        /// <param name="fromKey">wif or private key
        /// Example: WIF ("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"), PrivateKey ("450d6c2a04b5b470339a745427bae6828400cf048400837d73c415063835e005")</param>
        /// <param name="toAddress">address or account script hash</param>
        /// <param name="amount">token amount</param>
        /// <param name="networkFee">netwotk fee, set to be 0 will auto calculate the least fee</param>
        /// <returns></returns>
        public Transaction Transfer(string tokenHash, string fromKey, string toAddress, decimal amount, decimal networkFee = 0)
        {
            UInt160 scriptHash = Utility.GetScriptHash(tokenHash);
            var decimals = nep5API.Decimals(scriptHash);

            KeyPair from = Utility.GetKeyPair(fromKey);
            UInt160 to = Utility.GetScriptHash(toAddress);
            BigInteger amountInteger = amount.ToBigInteger(decimals);
            BigInteger networkFeeInteger = networkFee.ToBigInteger(NativeContract.GAS.Decimals);
            return Transfer(scriptHash, from, to, amountInteger, (long)networkFeeInteger);
        }

        /// <summary>
        /// Transfer NEP5 token balance
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="fromKey">from KeyPair</param>
        /// <param name="to">to account script hash</param>
        /// <param name="amount">transfer amount</param>
        /// <param name="networkFee">netwotk fee, set to be 0 will auto calculate the least fee</param>
        /// <returns></returns>
        public Transaction Transfer(UInt160 scriptHash, KeyPair from, UInt160 to, BigInteger amountInteger, BigInteger networkFeeInteger = default)
        {
            Transaction transaction = nep5API.CreateTransferTx(scriptHash, from, to, amountInteger, (long)networkFeeInteger);
            rpcClient.SendRawTransaction(transaction);
            return transaction;
        }

        /// <summary>
        /// Wait until the transaction is observable block chain
        /// </summary>
        /// <param name="transaction">the transaction to observe</param>
        /// <param name="timeout">TimeoutException throws after "timeout" seconds</param>
        /// <returns>the Transaction state, including vmState and blockhash</returns>
        public async Task<RpcTransaction> WaitTransaction(Transaction transaction, int timeout = 60)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeout);
            RpcTransaction rpcTx = null;
            while (rpcTx == null || rpcTx.Confirmations == null)
            {
                if (deadline < DateTime.UtcNow)
                {
                    throw new TimeoutException();
                }

                try
                {
                    rpcTx = rpcClient.GetRawTransaction(transaction.Hash.ToString());
                    if (rpcTx == null || rpcTx.Confirmations == null)
                    {
                        await Task.Delay((int)Blockchain.MillisecondsPerBlock / 2);
                    }
                }
                catch (Exception) { }
            }
            return rpcTx;
        }
    }
}
