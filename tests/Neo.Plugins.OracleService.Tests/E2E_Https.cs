// Copyright (C) 2015-2024 The Neo Project.
//
// UT_OracleService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Neo.Plugins.OracleService.Tests.TestBlockchain;
using static Neo.Plugins.OracleService.Tests.TestUtils;

namespace Neo.Plugins.OracleService.Tests
{
    [TestClass]
    public class E2E_Https : TestKit
    {
        UInt160 customContract;

        [TestInitialize]
        public void TestSetup()
        {
            customContract = InitializeContract();
        }

        [TestMethod]
        public void TestE2EHttps()
        {
            byte[] script;
            using (ScriptBuilder sb = new())
            {
                sb.EmitDynamicCall(NativeContract.RoleManagement.Hash, "designateAsRole",
                    [Role.Oracle,
                        new ContractParameter()
                        {
                            Type = ContractParameterType.Array,
                            Value = settings.StandbyCommittee.Select(
                            p => new ContractParameter() { Type = ContractParameterType.PublicKey, Value = p }).ToList()
                        }]);
                // Expected result: 12685221
                sb.EmitDynamicCall(customContract, "createRequest",
                    ["https://api.github.com/orgs/neo-project", "$.id", "callback", new byte[] { }, 1_0000_0000]);
                script = sb.ToArray();
            }
            Transaction[] txs = [
                new Transaction
                {
                    Nonce = 233,
                    ValidUntilBlock = NativeContract.Ledger.CurrentIndex(s_theNeoSystem.GetSnapshotCache()) + s_theNeoSystem.Settings.MaxValidUntilBlockIncrement,
                    Signers = [new Signer() { Account = MultisigScriptHash, Scopes = WitnessScope.CalledByEntry }],
                    Attributes = Array.Empty<TransactionAttribute>(),
                    Script = script,
                    NetworkFee = 1000_0000,
                    SystemFee = 2_0000_0000,
                }
            ];
            byte[] signature = txs[0].Sign(s_walletAccount.GetKey(), settings.Network);
            txs[0].Witnesses = [new Witness
            {
                InvocationScript = new byte[] { (byte)OpCode.PUSHDATA1, (byte)signature.Length }.Concat(signature).ToArray(),
                VerificationScript = MultisigScript,
            }];
            Block block = new Block
            {
                Header = new Header
                {
                    Version = 0,
                    PrevHash = s_theNeoSystem.GenesisBlock.Hash,
                    Timestamp = s_theNeoSystem.GenesisBlock.Timestamp + 15_000,
                    Index = 1,
                    NextConsensus = s_theNeoSystem.GenesisBlock.NextConsensus,
                },
                Transactions = txs,
            };
            block.Header.MerkleRoot ??= MerkleTree.ComputeRoot(block.Transactions.Select(t => t.Hash).ToArray());
            signature = block.Sign(s_walletAccount.GetKey(), settings.Network);
            block.Header.Witness = new Witness
            {
                InvocationScript = new byte[] { (byte)OpCode.PUSHDATA1, (byte)signature.Length }.Concat(signature).ToArray(),
                VerificationScript = MultisigScript,
            };
            s_theNeoSystem.Blockchain.Ask(block).Wait();
            Task t = s_oracle.Start(s_wallet);
            t.Wait(TimeSpan.FromMilliseconds(900));
            s_oracle.cancelSource.Cancel();
            t.Wait();
        }
    }
}
