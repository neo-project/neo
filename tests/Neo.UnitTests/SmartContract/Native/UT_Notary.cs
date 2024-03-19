// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Notary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM;
using Neo.Wallets;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Numerics;
using System.Numerics;
using System.Numerics;
using System.Threading.Tasks;
using VMTypes = Neo.VM.Types;
// using VMArray = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_Notary
    {
        private DataCache _snapshot;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshot = TestBlockchain.GetTestSnapshot();
            _persistingBlock = new Block { Header = new Header() };
        }

        [TestMethod]
        public void Check_Name() => NativeContract.Notary.Name.Should().Be(nameof(Notary));

        [TestMethod]
        public void Check_OnNEP17Payment()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();


            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            // Non-GAS transfer should fail.
            Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.NEO.Transfer(snapshot, from, NativeContract.Notary.Hash.ToArray(), BigInteger.Zero, true, persistingBlock));

            // GAS transfer with invalid data format should fail.
            // Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.GAS.Transfer(snapshot, from, NativeContract.Notary.Hash.ToArray(), BigInteger.Zero, true, persistingBlock, (uint)5));
        }

        [TestMethod]
        public void Check_GetMaxNotValidBeforeDelta()
        {
            const int defaultMaxNotValidBeforeDelta = 140;
            NativeContract.Notary.GetMaxNotValidBeforeDelta(_snapshot).Should().Be(defaultMaxNotValidBeforeDelta);
        }

        [TestMethod]
        public void Check_SetMaxNotValidBeforeDelta()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            using var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(committeeAddress), snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Notary.Hash, "setMaxNotValidBeforeDelta", 100);
            engine.LoadScript(script.ToArray());
            VMState vMState = engine.Execute();
            vMState.Should().Be(VMState.HALT);
            NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot).Should().Be(100);
        }

        [TestMethod]
        public void Check_OnPersist_NotaryRewards()
        {
            // Hardcode test values.
            const uint defaultNotaryssestedFeePerKey = 1000_0000;
            const byte NKeys1 = 4;
            const byte NKeys2 = 6;

            // Generate two transactions with NotaryAssisted attributes with hardcoded NKeys values.
            var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            var tx1 = TestUtils.GetTransaction(from);
            tx1.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys1 } };
            var netFee1 = 1_0000_0000;
            tx1.NetworkFee = netFee1;
            var tx2 = TestUtils.GetTransaction(from);
            tx2.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys2 } };
            var netFee2 = 2_0000_0000;
            tx2.NetworkFee = netFee2;

            // Calculate overall expected Notary nodes reward.
            var expectedNotaryReward = (NKeys1 + 1) * defaultNotaryssestedFeePerKey + (NKeys2 + 1) * defaultNotaryssestedFeePerKey;

            // Build block to check transaction fee distribution during Gas OnPersist.
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = new Transaction[] { tx1, tx2 }
            };
            var snapshot = _snapshot.CreateSnapshot();

            // Designate several Notary nodes.
            byte[] privateKey1 = new byte[32];
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            rng.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = NativeContract.RoleManagement.Call(
                snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                new Block { Header = new Header() },
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
                new ContractParameter(ContractParameterType.Array)
                {
                    Value = new List<ContractParameter>(){
                    new ContractParameter(ContractParameterType.ByteArray){Value = key1.PublicKey.ToArray()},
                    new ContractParameter(ContractParameterType.ByteArray){Value = key2.PublicKey.ToArray()},
                }
                }
            );
            snapshot.Commit();

            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            // Check that block's Primary balance is 0.
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            var primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            NativeContract.GAS.BalanceOf(engine.Snapshot, primary).Should().Be(0);

            // Execute OnPersist script.
            engine.LoadScript(script.ToArray());
            Assert.IsTrue(engine.Execute() == VMState.HALT);

            // Check that proper amount of GAS was minted to block's Primary and the rest
            // is evenly devided between designated Notary nodes as a reward.
            Assert.AreEqual(2 + 1 + 2, engine.Notifications.Count()); // burn tx1 and tx2 network fee + mint primary reward + transfer reward to Notary1 and Notary2
            Assert.AreEqual(netFee1 + netFee2 - expectedNotaryReward, engine.Notifications[2].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, primary).Should().Be(netFee1 + netFee2 - expectedNotaryReward);
            Assert.AreEqual(expectedNotaryReward / 2, engine.Notifications[3].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, key1.PublicKey.EncodePoint(true).ToScriptHash()).Should().Be(expectedNotaryReward / 2);
            Assert.AreEqual(expectedNotaryReward / 2, engine.Notifications[4].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, key2.PublicKey.EncodePoint(true).ToScriptHash()).Should().Be(expectedNotaryReward / 2);
        }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = NativeContract.GAS.Id,
                Key = buffer
            };
        }
    }
}
