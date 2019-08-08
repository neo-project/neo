using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SDK.SC;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
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

        public Transaction Tx { private set; get; }

        public TransactionContext Context { private set; get; }

        public TxManager(RpcClient neoRpc, UInt160 sender)
        {
            rpcClient = neoRpc;
            this.sender = sender;
        }

        /// <summary>
        /// Create an unsigned Transaction object with given parameters.
        /// </summary>
        public TxManager MakeTransaction(TransactionAttribute[] attributes, byte[] script, long networkFee)
        {
            uint height = rpcClient.GetBlockCount() - 1;
            Tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)rand.Next(),
                Script = script,
                Sender = sender,
                ValidUntilBlock = height + Transaction.MaxValidUntilBlockIncrement,
                Attributes = attributes ?? new TransactionAttribute[0],
                Witnesses = new Witness[0]
            };

            RpcInvokeResult result = rpcClient.InvokeScript(script);
            Tx.SystemFee = Math.Max(long.Parse(result.GasConsumed) - ApplicationEngine.GasFree, 0);
            if (Tx.SystemFee > 0)
            {
                long d = (long)NativeContract.GAS.Factor;
                long remainder = Tx.SystemFee % d;
                if (remainder > 0)
                    Tx.SystemFee += d - remainder;
                else if (remainder < 0)
                    Tx.SystemFee -= remainder;
            }
            UInt160[] hashes = Tx.GetScriptHashesForVerifying(null);
            int size = Transaction.HeaderSize + attributes.GetVarSize() + script.GetVarSize() + IO.Helper.GetVarSize(hashes.Length);
            long feePerByte = new PolicyAPI(rpcClient).GetFeePerByte();
            long leastNetworkFee = size * feePerByte;

            Tx.NetworkFee = networkFee;
            Context = new TransactionContext(Tx);

            var gasBalance = new Nep5API(rpcClient).BalanceOf(NativeContract.GAS.Hash, sender);
            if (gasBalance >= Tx.SystemFee + Tx.NetworkFee && Tx.NetworkFee >= leastNetworkFee) return this;
            throw new InvalidOperationException("Insufficient GAS");
        }

        /// <summary>
        /// Add Signature
        /// </summary>
        public TxManager AddSignature(KeyPair key)
        {
            var contract = Contract.CreateSignatureContract(key.PublicKey);

            byte[] signature = Tx.Sign(key);
            if (!Context.AddSignature(contract, key.PublicKey, signature))
            {
                throw new Exception("AddSignature failed!");
            }

            return this;
        }

        /// <summary>
        /// Add Multi-Signature
        /// </summary>
        public TxManager AddMultiSig(KeyPair key, params ECPoint[] publicKeys)
        {
            Contract contract = Contract.CreateMultiSigContract(publicKeys.Length, publicKeys);

            byte[] signature = Tx.Sign(key);
            if (!Context.AddSignature(contract, key.PublicKey, signature))
            {
                throw new Exception("AddMultiSig failed!");
            }

            return this;
        }

        /// <summary>
        /// Add Witness with contract
        /// </summary>
        public TxManager AddWitness(Contract contract, params object[] parameters)
        {
            if (!Context.Add(contract, parameters))
            {
                throw new Exception("AddWitness failed!");
            };
            return this;
        }

        /// <summary>
        /// Add Witness with scriptHash
        /// </summary>
        public TxManager AddWitness(UInt160 scriptHash, params object[] parameters)
        {
            var contract = Contract.Create(scriptHash);
            return AddWitness(contract, parameters);
        }

        /// <summary>
        /// Verify Witness count and add witnesses
        /// </summary>
        public TxManager Sign()
        {
            Tx.Witnesses = Context.GetWitnesses();
            return this;
        }
    }
}
