using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SDK.SC;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;

namespace Neo.SDK.TX
{
    /// <summary>
    /// This class helps to create transactions manually.
    /// </summary>
    public class TxManager
    {
        private static readonly Random rand = new Random();
        private readonly RpcClient rpcClient;
        private readonly UInt160 sender;

        public TxManager(RpcClient neoRpc, UInt160 sender)
        {
            rpcClient = neoRpc;
            this.sender = sender;
        }

        /// <summary>
        /// Create an unsigned Transaction object with given parameters.
        /// </summary>
        public Transaction MakeTransaction(TransactionAttribute[] attributes, byte[] script)
        {
            uint height = rpcClient.GetBlockCount() - 1;
            Transaction tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = script,
                Sender = sender,
                ValidUntilBlock = height + Transaction.MaxValidUntilBlockIncrement,
                Attributes = attributes
            };

            RpcInvokeResult result = rpcClient.InvokeScript(script);
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

            long feePerByte = new PolicyAPI(rpcClient).GetFeePerByte();
            tx.NetworkFee += size * feePerByte;
            var gasBalance = new Nep5API(rpcClient).BalanceOf(NativeContract.GAS.Hash, sender);
            if (gasBalance >= tx.SystemFee + tx.NetworkFee) return tx;
            throw new InvalidOperationException("Insufficient GAS");
        }

    }
}
