// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ContractClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System.Threading.Tasks;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_ContractClient
    {
        Mock<RpcClient> rpcClientMock;
        KeyPair keyPair1;
        UInt160 sender;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair1 = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            sender = Contract.CreateSignatureRedeemScript(keyPair1.PublicKey).ToScriptHash();
            rpcClientMock = UT_TransactionManager.MockRpcClient(sender, []);
        }

        [TestMethod]
        public async Task TestInvoke()
        {
            byte[] testScript = NativeContract.GAS.Hash.MakeScript("balanceOf", UInt160.Zero);
            UT_TransactionManager.MockInvokeScript(rpcClientMock, testScript, new ContractParameter { Type = ContractParameterType.ByteArray, Value = "00e057eb481b".HexToBytes() });

            ContractClient contractClient = new ContractClient(rpcClientMock.Object);
            var result = await contractClient.TestInvokeAsync(NativeContract.GAS.Hash, "balanceOf", UInt160.Zero);

            Assert.AreEqual(30000000000000L, (long)result.Stack[0].GetInteger());
        }

        [TestMethod]
        public async Task TestDeployContract()
        {
            byte[] script;
            var manifest = new ContractManifest()
            {
                Permissions = [ContractPermission.DefaultPermission],
                Abi = new ContractAbi()
                {
                    Events = [],
                    Methods = []
                },
                Groups = [],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                SupportedStandards = ["NEP-10"],
                Extra = null,
            };
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitDynamicCall(NativeContract.ContractManagement.Hash, "deploy", new byte[1], manifest.ToJson().ToString());
                script = sb.ToArray();
            }

            UT_TransactionManager.MockInvokeScript(rpcClientMock, script, new ContractParameter());

            ContractClient contractClient = new ContractClient(rpcClientMock.Object);
            var result = await contractClient.CreateDeployContractTxAsync(new byte[1], manifest, keyPair1);

            Assert.IsNotNull(result);
        }
    }
}
