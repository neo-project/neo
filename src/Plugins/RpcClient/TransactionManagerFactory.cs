// Copyright (C) 2015-2024 The Neo Project.
//
// TransactionManagerFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using System;
using System.Threading.Tasks;

namespace Neo.Network.RPC
{
    public class TransactionManagerFactory
    {
        private readonly RpcClient rpcClient;

        /// <summary>
        /// TransactionManagerFactory Constructor
        /// </summary>
        /// <param name="rpcClient">the RPC client to call NEO RPC API</param>
        public TransactionManagerFactory(RpcClient rpcClient)
        {
            this.rpcClient = rpcClient;
        }

        /// <summary>
        /// Create an unsigned Transaction object with given parameters.
        /// </summary>
        /// <param name="script">Transaction Script</param>
        /// <param name="signers">Transaction Signers</param>
        /// <param name="attributes">Transaction Attributes</param>
        /// <returns></returns>
        public async Task<TransactionManager> MakeTransactionAsync(ReadOnlyMemory<byte> script, Signer[] signers = null, TransactionAttribute[] attributes = null)
        {
            RpcInvokeResult invokeResult = await rpcClient.InvokeScriptAsync(script, signers).ConfigureAwait(false);
            return await MakeTransactionAsync(script, invokeResult.GasConsumed, signers, attributes).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an unsigned Transaction object with given parameters.
        /// </summary>
        /// <param name="script">Transaction Script</param>
        /// <param name="systemFee">Transaction System Fee</param>
        /// <param name="signers">Transaction Signers</param>
        /// <param name="attributes">Transaction Attributes</param>
        /// <returns></returns>
        public async Task<TransactionManager> MakeTransactionAsync(ReadOnlyMemory<byte> script, long systemFee, Signer[] signers = null, TransactionAttribute[] attributes = null)
        {
            uint blockCount = await rpcClient.GetBlockCountAsync().ConfigureAwait(false) - 1;

            var tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)new Random().Next(),
                Script = script,
                Signers = signers ?? Array.Empty<Signer>(),
                ValidUntilBlock = blockCount - 1 + rpcClient.protocolSettings.MaxValidUntilBlockIncrement,
                SystemFee = systemFee,
                Attributes = attributes ?? Array.Empty<TransactionAttribute>(),
            };

            return new TransactionManager(tx, rpcClient);
        }
    }
}
