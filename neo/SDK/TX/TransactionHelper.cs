using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SDK.RPC;
using Neo.SDK.RPC.Model;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Neo.Persistence;
using Neo.SmartContract.Native;

namespace Neo.SDK.TX
{
    /// <summary>
    /// This class helps to create transactions manually.
    /// </summary>
    public class TransactionHelper
    {
        private static readonly Random rand = new Random();
        private readonly IRpcClient _neoRpc;
        private readonly UInt160 _sender;

        public TransactionHelper(IRpcClient neoRpc, UInt160 sender)
        {
            _neoRpc = neoRpc;
            _sender = sender;
        }

        /// <summary>
        /// Call API method to get the balance of a specific NEP-5 token from a specific address.
        /// </summary>
        protected BigInteger GetTokenBalance(UInt160 assetId, UInt160 from)
        {
            string fromStr = from.ToAddress();
            Nep5Balance nep5Balance = _neoRpc.GetNep5Balances(fromStr).Balances.Where(n => UInt160.Parse(n.AssetHash) == assetId).FirstOrDefault();
            if (nep5Balance == default || !BigInteger.TryParse(nep5Balance.Amount, out BigInteger balance))
                return 0;
            return balance;
        }

        protected uint GetBlockHeight()
        {
            return 0;
        }

        public Transaction Transfer(UInt160 assetId, UInt160 from, UInt160 to, BigInteger amount)
        {
            if (from != _sender) return null;
            BigInteger balance = GetTokenBalance(assetId, from);
            if (balance < amount) return null;
            
            BigInteger balance_gas = BigInteger.Zero;
            if (assetId.Equals(NativeContract.GAS.Hash))
            {
                balance_gas = balance - amount;
                if (balance_gas <= 0) return null;
            }

            byte[] script = this.GenerateScript(assetId, "transfer", from, to, amount);
            Transaction tx = this.CreateTransaction(script, from);
            BigInteger fee = tx.Gas + tx.NetworkFee;
            if (balance_gas == BigInteger.Zero)
                balance_gas = GetTokenBalance(NativeContract.GAS.Hash, from);
            if (balance_gas < fee) return null;
            return tx;
        }

        public Transaction BalanceOf(UInt160 assetId, UInt160 account)
        {
            byte[] script = this.GenerateScript(assetId, "balanceOf", account);
            Transaction tx = this.CreateTransaction(script, _sender);
            return tx;
        }


        //deploy contract


        //invoke contract


        public Transaction CreateTransaction(byte[] script, UInt160 sender, IEnumerable<TransactionAttribute> attributes = null)
        {
            TransactionAttribute[] attr = attributes != null ? attributes.ToArray() : new TransactionAttribute[0];
            Transaction tx = new Transaction
            {
                Script = script,
                Sender = sender,
                Attributes = attr
            };
            tx = this.FillTransactionHelper(tx, sender);
            return tx;
        }

        public byte[] GenerateScript(UInt160 scriptHash, string operation, params object[] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitAppCall(scriptHash, operation, args);
                return sb.ToArray();
            }
        }

        private void CalculateFeesHelper(Transaction tx)
        {
            if (tx.Sender is null) tx.Sender = UInt160.Zero;
            if (tx.Attributes is null) tx.Attributes = new TransactionAttribute[0];
            if (tx.Witness is null) tx.Witness = new Witness
            {
                InvocationScript = new byte[65],
                VerificationScript = new byte[39]
            };

            //tx.Hash
            long consumed;
            using (ApplicationEngine engine = ApplicationEngine.Run(tx.Script, tx))
            {
                if (engine.State.HasFlag(VMState.FAULT))
                    throw new InvalidOperationException();
                consumed = engine.GasConsumed;
            }

            long factor = (long)NativeContract.GAS.Factor; // Gas: 100,000,000
            tx.Gas = consumed - ApplicationEngine.GasFree;
            if (tx.Gas <= 0)
            {
                tx.Gas = 0; // free 
            }
            else
            {
                long remainder = tx.Gas % factor;
                if (remainder == 0) return;
                if (remainder > 0)
                    tx.Gas += factor - remainder; // why
                else
                    tx.Gas -= remainder;
            }
            long feePerByte = 1000L;
            long fee = feePerByte * tx.Size;
            if (fee > tx.NetworkFee)
                tx.NetworkFee = fee;
        }

        private Transaction FillTransactionHelper(Transaction tx, UInt160 sender)
        {
            if (tx.Version != 0)
                tx.Version = 0;
            if (tx.Nonce == 0)
                tx.Nonce = (uint)rand.Next();
            if (tx.ValidUntilBlock == 0)
                tx.ValidUntilBlock = GetBlockHeight() + Transaction.MaxValidUntilBlockIncrement;

            this.CalculateFeesHelper(tx);
            BigInteger fee = tx.Gas + tx.NetworkFee;
            BigInteger balance = GetTokenBalance(NativeContract.GAS.Hash, sender);
            if (balance >= fee)
            {
                tx.Sender = sender;
                return tx;
            }
            
            throw new InvalidOperationException();
        }
    }
}