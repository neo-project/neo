// Copyright (C) 2015-2024 The Neo Project.
//
// ContractClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Threading.Tasks;

namespace Neo.Network.RPC
{
    /// <summary>
    /// Contract related operations through RPC API
    /// </summary>
    public class ContractClient
    {
        protected readonly RpcClient rpcClient;

        /// <summary>
        /// ContractClient Constructor
        /// </summary>
        /// <param name="rpc">the RPC client to call NEO RPC methods</param>
        public ContractClient(RpcClient rpc)
        {
            rpcClient = rpc;
        }

        /// <summary>
        /// Use RPC method to test invoke operation.
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="operation">contract operation</param>
        /// <param name="args">operation arguments</param>
        /// <returns></returns>
        public Task<RpcInvokeResult> TestInvokeAsync(UInt160 scriptHash, string operation, params object[] args)
        {
            byte[] script = scriptHash.MakeScript(operation, args);
            return rpcClient.InvokeScriptAsync(script);
        }

        /// <summary>
        /// Deploy Contract, return signed transaction
        /// </summary>
        /// <param name="nefFile">neo contract executable file</param>
        /// <param name="manifest">contract manifest</param>
        /// <param name="key">sender KeyPair</param>
        /// <returns></returns>
        public async Task<Transaction> CreateDeployContractTxAsync(byte[] nefFile, ContractManifest manifest, KeyPair key)
        {
            byte[] script;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", nefFile, manifest.ToJson().ToString());
                script = sb.ToArray();
            }
            UInt160 sender = Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
            Signer[] signers = [new Signer { Scopes = WitnessScope.CalledByEntry, Account = sender }];

            TransactionManagerFactory factory = new TransactionManagerFactory(rpcClient);
            TransactionManager manager = await factory.MakeTransactionAsync(script, signers).ConfigureAwait(false);
            return await manager
                .AddSignature(key)
                .SignAsync().ConfigureAwait(false);
        }
    }
}
