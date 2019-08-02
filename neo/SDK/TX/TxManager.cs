using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.IO;

namespace Neo.SDK.TX
{
    /// <summary>
    /// This class helps to create transactions manually.
    /// </summary>
    public class TxManager
    {
        public long FeePerByte { get; set; }

        private static readonly Random rand = new Random();
        private readonly RpcClient _neoRpc;
        private readonly UInt160 _sender;
        private readonly BigInteger _gasBalance;

        public TxManager(RpcClient neoRpc, UInt160 sender, long feePerByte = 1000)
        {
            FeePerByte = feePerByte;
            _neoRpc = neoRpc;
            _sender = sender;
            _gasBalance = GetTokenBalance(NativeContract.GAS.Hash, _sender);
        }
        
        /// <summary>
        /// Create an unsigned Transaction object with given parameters.
        /// </summary>
        public Transaction MakeTransaction(TransactionAttribute[] attributes, byte[] script)
        {
            uint height = (uint)_neoRpc.GetBlockCount();
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = script,
                Sender = _sender,
                ValidUntilBlock = height + Transaction.MaxValidUntilBlockIncrement,
                Attributes = attributes
            };
            
            // call rpc to test run 
            RpcInvokeResult result = _neoRpc.InvokeScript(script.ToHexString());
            tx.SystemFee = Math.Max(long.Parse(result.GasConsumed) - ApplicationEngine.GasFree, 0);
            if (tx.SystemFee > 0)
            {
                long d = (long)NativeContract.GAS.Factor;
                long remainder = tx.SystemFee % d;
                if (remainder > 0)
                    tx.SystemFee += d - remainder;
                else if (remainder < 0)
                    tx.SystemFee -= remainder;
            }
            UInt160[] hashes = tx.GetScriptHashesForVerifying(null);
            int size = Transaction.HeaderSize + attributes.GetVarSize() + script.GetVarSize() + IO.Helper.GetVarSize(hashes.Length);
            
            tx.NetworkFee += size * FeePerByte; // FeePerByte:1000, hard coded here
            if (_gasBalance >= tx.SystemFee + tx.NetworkFee) return tx;
            throw new InvalidOperationException("Insufficient GAS");
        }

        /// <summary>
        /// Generate scripts to call a specific method from a specific contract.
        /// </summary>
        public byte[] MakeScript(UInt160 scriptHash, string operation, params object[] args)
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
        /// Use RPC method to get the balance of a specific NEP-5 token from a specific address.
        /// </summary>
        private BigInteger GetTokenBalance(UInt160 assetId, UInt160 account)
        {
            byte[] balanceScript = MakeScript(NativeContract.GAS.Hash, "balanceOf", account);
            BigInteger balance = BigInteger.Parse(_neoRpc.InvokeScript(balanceScript.ToHexString()).Stack.Single().Value);
            return balance;
        }
    }
}
