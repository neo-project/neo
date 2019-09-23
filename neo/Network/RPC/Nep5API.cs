using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System.Linq;
using System.Numerics;

namespace Neo.Network.RPC
{
    /// <summary>
    /// Call NEP5 methods with RPC API
    /// </summary>
    public class Nep5API : ContractClient
    {
        /// <summary>
        /// Nep5API Constructor
        /// </summary>
        /// <param name="rpcClient">the RPC client to call NEO RPC methods</param>
        public Nep5API(RpcClient rpcClient) : base(rpcClient) { }

        /// <summary>
        /// Get balance of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="account">account script hash</param>
        /// <returns></returns>
        public BigInteger BalanceOf(UInt160 scriptHash, UInt160 account)
        {
            BigInteger balance = TestInvoke(scriptHash, "balanceOf", account).Stack.Single().ToStackItem().GetBigInteger();
            return balance;
        }

        /// <summary>
        /// Get name of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public string Name(UInt160 scriptHash)
        {
            return TestInvoke(scriptHash, "name").Stack.Single().ToStackItem().GetString();
        }

        /// <summary>
        /// Get symbol of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public string Symbol(UInt160 scriptHash)
        {
            return TestInvoke(scriptHash, "symbol").Stack.Single().ToStackItem().GetString();
        }

        /// <summary>
        /// Get decimals of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public uint Decimals(UInt160 scriptHash)
        {
            return (uint)TestInvoke(scriptHash, "decimals").Stack.Single().ToStackItem().GetBigInteger();
        }

        /// <summary>
        /// Get total supply of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <returns></returns>
        public BigInteger TotalSupply(UInt160 scriptHash)
        {
            return TestInvoke(scriptHash, "totalSupply").Stack.Single().ToStackItem().GetBigInteger();
        }

        /// <summary>
        /// Get name of NEP5 token
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="fromKey">from KeyPair</param>
        /// <param name="to">to account script hash</param>
        /// <param name="amount">transfer amount</param>
        /// <param name="networkFee">netwotk fee, set to be 0 if you don't need higher priority</param>
        /// <returns></returns>
        public Transaction Transfer(UInt160 scriptHash, KeyPair fromKey, UInt160 to, BigInteger amount, long networkFee = 0)
        {
            var sender = Contract.CreateSignatureRedeemScript(fromKey.PublicKey).ToScriptHash();
            Cosigner[] cosigners = new[] { new Cosigner { Scopes = WitnessScope.CalledByEntry, Account = sender } };

            byte[] script = scriptHash.MakeScript("transfer", sender, to, amount);
            Transaction tx = new TransactionManager(rpcClient, sender)
                .MakeTransaction(script, null, cosigners, networkFee)
                .AddSignature(fromKey)
                .Sign()
                .Tx;

            return tx;
        }
    }
}
