// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using static Neo.SmartContract.Native.NeoToken;
using Array = System.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NeoToken
    {
        private DataCache _snapshotCache;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            _persistingBlock = new Block
            {
                Header = new Header(),
                Transactions = Array.Empty<Transaction>()
            };
        }

        [TestMethod]
        public void Check_Name() => Assert.AreEqual(nameof(NeoToken), NativeContract.NEO.Name);

        [TestMethod]
        public void Check_Symbol() => Assert.AreEqual("NEO", NativeContract.NEO.Symbol(_snapshotCache));

        [TestMethod]
        public void Check_Decimals() => Assert.AreEqual(0, NativeContract.NEO.Decimals(_snapshotCache));

        [TestMethod]
        public void Test_HF_EchidnaStates()
        {
            string json = UT_ProtocolSettings.CreateHFSettings("\"HF_Echidna\": 10");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var settings = ProtocolSettings.Load(stream);

            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header() };

            foreach (var method in new[] { "vote", "registerCandidate", "unregisterCandidate", "getGasPerBlock" })
            {
                // Test WITHOUT HF_Echidna

                persistingBlock.Header.Index = 9;

                using (var engine = ApplicationEngine.Create(TriggerType.Application,
                    new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), clonedCache, persistingBlock, settings: settings))
                {
                    var methods = NativeContract.NEO.GetContractMethods(engine);
                    var entries = methods.Values.Where(u => u.Name == method).ToArray();

                    if (method == "getGasPerBlock")
                    {
                        Assert.AreEqual(2, entries.Length);
                        Assert.AreEqual(0, entries.First().Parameters.Length);
                        Assert.AreEqual(1, entries.Skip(1).First().Parameters.Length);
                        Assert.AreEqual(CallFlags.ReadStates, entries[0].RequiredCallFlags);
                        Assert.AreEqual(CallFlags.ReadStates | CallFlags.WriteStates, entries[1].RequiredCallFlags);
                    }
                    else
                    {
                        Assert.AreEqual(1, entries.Length);
                        Assert.AreEqual(CallFlags.States, entries[0].RequiredCallFlags);
                    }
                }

                // Test WITH HF_Echidna

                persistingBlock.Header.Index = 10;

                using (var engine = ApplicationEngine.Create(TriggerType.Application,
                     new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), clonedCache, persistingBlock, settings: settings))
                {
                    var methods = NativeContract.NEO.GetContractMethods(engine);
                    var entries = methods.Values.Where(u => u.Name == method).ToArray();

                    if (method == "getGasPerBlock")
                    {
                        Assert.AreEqual(2, entries.Length);
                        Assert.AreEqual(0, entries.First().Parameters.Length);
                        Assert.AreEqual(1, entries.Skip(1).First().Parameters.Length);
                        Assert.AreEqual(CallFlags.ReadStates, entries[0].RequiredCallFlags);
                        Assert.AreEqual(CallFlags.ReadStates | CallFlags.WriteStates, entries[1].RequiredCallFlags);
                    }
                    else
                    {
                        Assert.AreEqual(1, entries.Length);
                        Assert.AreEqual(CallFlags.States | CallFlags.AllowNotify, entries[0].RequiredCallFlags);
                    }
                }
            }
        }

        [TestMethod]
        public void Check_Vote()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

            // No signature

            var ret = Check_Vote(clonedCache, from, null, false, persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsTrue(ret.State);

            // Wrong address

            ret = Check_Vote(clonedCache, new byte[19], null, false, persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsFalse(ret.State);

            // Wrong ec

            ret = Check_Vote(clonedCache, from, new byte[19], true, persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsFalse(ret.State);

            // no registered

            var fakeAddr = new byte[20];
            fakeAddr[0] = 0x5F;
            fakeAddr[5] = 0xFF;

            ret = Check_Vote(clonedCache, fakeAddr, null, true, persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsTrue(ret.State);

            // no registered

            var accountState = clonedCache.TryGet(CreateStorageKey(20, from)).GetInteroperable<NeoAccountState>();
            accountState.VoteTo = null;
            ret = Check_Vote(clonedCache, from, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsTrue(ret.State);
            Assert.IsNull(accountState.VoteTo);

            // normal case

            clonedCache.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState() { Registered = true }));
            ret = Check_Vote(clonedCache, from, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            accountState = clonedCache.TryGet(CreateStorageKey(20, from)).GetInteroperable<NeoAccountState>();
            Assert.AreEqual(ECCurve.Secp256r1.G, accountState.VoteTo);
        }

        [TestMethod]
        public void Check_Vote_Sameaccounts()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
            var accountState = clonedCache.TryGet(CreateStorageKey(20, from)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            clonedCache.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState() { Registered = true }));
            var ret = Check_Vote(clonedCache, from, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            accountState = clonedCache.TryGet(CreateStorageKey(20, from)).GetInteroperable<NeoAccountState>();
            Assert.AreEqual(ECCurve.Secp256r1.G, accountState.VoteTo);

            //two account vote for the same account
            var stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            Assert.AreEqual(100, stateValidator.Votes);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState { Balance = 200 }));
            var secondAccount = clonedCache.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            Assert.AreEqual(200, secondAccount.Balance);
            ret = Check_Vote(clonedCache, G_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            Assert.AreEqual(300, stateValidator.Votes);
        }

        [TestMethod]
        public void Check_Vote_ChangeVote()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            //from vote to G
            byte[] from = TestProtocolSettings.Default.StandbyValidators[0].ToArray();
            var from_Account = Contract.CreateSignatureContract(TestProtocolSettings.Default.StandbyValidators[0]).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, from_Account), new StorageItem(new NeoAccountState()));
            var accountState = clonedCache.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            clonedCache.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState() { Registered = true }));
            var ret = Check_Vote(clonedCache, from_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            accountState = clonedCache.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            Assert.AreEqual(ECCurve.Secp256r1.G, accountState.VoteTo);

            //from change vote to itself
            var G_stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            Assert.AreEqual(100, G_stateValidator.Votes);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState { Balance = 200 }));
            clonedCache.Add(CreateStorageKey(33, from), new StorageItem(new CandidateState() { Registered = true }));
            ret = Check_Vote(clonedCache, from_Account, from, true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            G_stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            Assert.AreEqual(0, G_stateValidator.Votes);
            var from_stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, from)).GetInteroperable<CandidateState>();
            Assert.AreEqual(100, from_stateValidator.Votes);
        }

        [TestMethod]
        public void Check_Vote_VoteToNull()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            byte[] from = TestProtocolSettings.Default.StandbyValidators[0].ToArray();
            var from_Account = Contract.CreateSignatureContract(TestProtocolSettings.Default.StandbyValidators[0]).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, from_Account), new StorageItem(new NeoAccountState()));
            var accountState = clonedCache.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            clonedCache.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState() { Registered = true }));
            clonedCache.Add(CreateStorageKey(23, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new BigInteger(100500)));
            var ret = Check_Vote(clonedCache, from_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            accountState = clonedCache.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            Assert.AreEqual(ECCurve.Secp256r1.G, accountState.VoteTo);
            Assert.AreEqual(100500, accountState.LastGasPerVote);

            //from vote to null account G votes becomes 0
            var G_stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            Assert.AreEqual(100, G_stateValidator.Votes);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState { Balance = 200 }));
            clonedCache.Add(CreateStorageKey(33, from), new StorageItem(new CandidateState() { Registered = true }));
            ret = Check_Vote(clonedCache, from_Account, null, true, persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            G_stateValidator = clonedCache.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            Assert.AreEqual(0, G_stateValidator.Votes);
            accountState = clonedCache.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            Assert.IsNull(accountState.VoteTo);
            Assert.AreEqual(0, accountState.LastGasPerVote);
        }

        [TestMethod]
        public void Check_UnclaimedGas()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

            var unclaim = Check_UnclaimedGas(clonedCache, from, persistingBlock);
            Assert.AreEqual(new BigInteger(0.5 * 1000 * 100000000L), unclaim.Value);
            Assert.IsTrue(unclaim.State);

            unclaim = Check_UnclaimedGas(clonedCache, new byte[19], persistingBlock);
            Assert.AreEqual(BigInteger.Zero, unclaim.Value);
            Assert.IsFalse(unclaim.State);
        }

        [TestMethod]
        public void Check_RegisterValidator()
        {
            var clonedCache = _snapshotCache.CloneCache();

            var keyCount = clonedCache.GetChangeSet().Count();
            var point = TestProtocolSettings.Default.StandbyValidators[0].EncodePoint(true).Clone() as byte[];

            var ret = Check_RegisterValidator(clonedCache, point, _persistingBlock); // Exists
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            Assert.AreEqual(++keyCount, clonedCache.GetChangeSet().Count()); // No changes

            point[20]++; // fake point
            ret = Check_RegisterValidator(clonedCache, point, _persistingBlock); // New

            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            Assert.AreEqual(keyCount + 1, clonedCache.GetChangeSet().Count()); // New validator

            // Check GetRegisteredValidators

            var members = NativeContract.NEO.GetCandidatesInternal(clonedCache);
            Assert.AreEqual(2, members.Count());
        }

        [TestMethod]
        public void Check_RegisterValidatorViaNEP27()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var point = ECPoint.Parse("021821807f923a3da004fb73871509d7635bcc05f41edef2a3ca5c941d8bbc1231", ECCurve.Secp256r1);
            var pointData = point.EncodePoint(true);

            // Send some NEO, shouldn't be accepted
            var ret = Check_RegisterValidatorViaNEP27(clonedCache, point, _persistingBlock, true, pointData, 1000_0000_0000);
            Assert.IsFalse(ret.State);

            // Send improper amount of GAS, shouldn't be accepted.
            ret = Check_RegisterValidatorViaNEP27(clonedCache, point, _persistingBlock, false, pointData, 1000_0000_0001);
            Assert.IsFalse(ret.State);

            // Broken witness.
            var badPoint = ECPoint.Parse("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", ECCurve.Secp256r1);
            ret = Check_RegisterValidatorViaNEP27(clonedCache, point, _persistingBlock, false, badPoint.EncodePoint(true), 1000_0000_0000);
            Assert.IsFalse(ret.State);

            // Successful case.
            ret = Check_RegisterValidatorViaNEP27(clonedCache, point, _persistingBlock, false, pointData, 1000_0000_0000);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            // Check GetRegisteredValidators
            var members = NativeContract.NEO.GetCandidatesInternal(clonedCache);
            Assert.AreEqual(1, members.Count());
            Assert.AreEqual(point, members.First().PublicKey);

            // No GAS should be left on the NEO account.
            Assert.AreEqual(0, NativeContract.GAS.BalanceOf(clonedCache, NativeContract.NEO.Hash));
        }

        [TestMethod]
        public void Check_UnregisterCandidate()
        {
            var clonedCache = _snapshotCache.CloneCache();
            _persistingBlock.Header.Index = 1;
            var keyCount = clonedCache.GetChangeSet().Count();
            var point = TestProtocolSettings.Default.StandbyValidators[0].EncodePoint(true);

            //without register
            var ret = Check_UnregisterCandidate(clonedCache, point, _persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            Assert.AreEqual(keyCount, clonedCache.GetChangeSet().Count());

            //register and then unregister
            ret = Check_RegisterValidator(clonedCache, point, _persistingBlock);
            StorageItem item = clonedCache.GetAndChange(CreateStorageKey(33, point));
            Assert.AreEqual(7, item.Size);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            var members = NativeContract.NEO.GetCandidatesInternal(clonedCache);
            Assert.AreEqual(1, members.Count());
            Assert.AreEqual(keyCount + 1, clonedCache.GetChangeSet().Count());
            StorageKey key = CreateStorageKey(33, point);
            Assert.IsNotNull(clonedCache.TryGet(key));

            ret = Check_UnregisterCandidate(clonedCache, point, _persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            Assert.AreEqual(keyCount, clonedCache.GetChangeSet().Count());

            members = NativeContract.NEO.GetCandidatesInternal(clonedCache);
            Assert.AreEqual(0, members.Count());
            Assert.IsNull(clonedCache.TryGet(key));

            //register with votes, then unregister
            ret = Check_RegisterValidator(clonedCache, point, _persistingBlock);
            Assert.IsTrue(ret.State);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState()));
            var accountState = clonedCache.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            Check_Vote(clonedCache, G_Account, TestProtocolSettings.Default.StandbyValidators[0].ToArray(), true, _persistingBlock);
            ret = Check_UnregisterCandidate(clonedCache, point, _persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);
            Assert.IsNotNull(clonedCache.TryGet(key));
            StorageItem pointItem = clonedCache.TryGet(key);
            CandidateState pointState = pointItem.GetInteroperable<CandidateState>();
            Assert.IsFalse(pointState.Registered);
            Assert.AreEqual(100, pointState.Votes);

            //vote fail
            ret = Check_Vote(clonedCache, G_Account, TestProtocolSettings.Default.StandbyValidators[0].ToArray(), true, _persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsFalse(ret.Result);
            accountState = clonedCache.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            Assert.AreEqual(TestProtocolSettings.Default.StandbyValidators[0], accountState.VoteTo);
        }

        [TestMethod]
        public void Check_GetCommittee()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var keyCount = clonedCache.GetChangeSet().Count();
            var point = TestProtocolSettings.Default.StandbyValidators[0].EncodePoint(true);
            var persistingBlock = _persistingBlock;
            persistingBlock.Header.Index = 1;
            //register with votes with 20000000
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            clonedCache.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState()));
            var accountState = clonedCache.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 20000000;
            var ret = Check_RegisterValidator(clonedCache, ECCurve.Secp256r1.G.ToArray(), persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);
            ret = Check_Vote(clonedCache, G_Account, ECCurve.Secp256r1.G.ToArray(), true, persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);


            var committeemembers = NativeContract.NEO.GetCommittee(clonedCache);
            var defaultCommittee = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray();
            Assert.AreEqual(committeemembers.GetType(), typeof(ECPoint[]));
            for (int i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount; i++)
            {
                Assert.AreEqual(committeemembers[i], defaultCommittee[i]);
            }

            //register more candidates, committee member change
            persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = Array.Empty<Transaction>()
            };
            for (int i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount - 1; i++)
            {
                ret = Check_RegisterValidator(clonedCache, TestProtocolSettings.Default.StandbyCommittee[i].ToArray(), persistingBlock);
                Assert.IsTrue(ret.State);
                Assert.IsTrue(ret.Result);
            }

            Assert.IsTrue(Check_OnPersist(clonedCache, persistingBlock));

            committeemembers = NativeContract.NEO.GetCommittee(clonedCache);
            Assert.AreEqual(committeemembers.Length, TestProtocolSettings.Default.CommitteeMembersCount);
            Assert.IsTrue(committeemembers.Contains(ECCurve.Secp256r1.G));
            for (int i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount - 1; i++)
            {
                Assert.IsTrue(committeemembers.Contains(TestProtocolSettings.Default.StandbyCommittee[i]));
            }
            Assert.IsFalse(committeemembers.Contains(TestProtocolSettings.Default.StandbyCommittee[TestProtocolSettings.Default.CommitteeMembersCount - 1]));
        }

        [TestMethod]
        public void Check_Transfer()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };

            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
            byte[] to = new byte[20];

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            var keyCount = clonedCache.GetChangeSet().Count();

            // Check unclaim

            var unclaim = Check_UnclaimedGas(clonedCache, from, persistingBlock);
            Assert.AreEqual(new BigInteger(0.5 * 1000 * 100000000L), unclaim.Value);
            Assert.IsTrue(unclaim.State);

            // Transfer

            Assert.IsFalse(NativeContract.NEO.Transfer(clonedCache, from, to, BigInteger.One, false, persistingBlock)); // Not signed
            Assert.IsTrue(NativeContract.NEO.Transfer(clonedCache, from, to, BigInteger.One, true, persistingBlock));
            Assert.AreEqual(99999999, NativeContract.NEO.BalanceOf(clonedCache, from));
            Assert.AreEqual(1, NativeContract.NEO.BalanceOf(clonedCache, to));

            var (from_balance, _, _) = GetAccountState(clonedCache, new UInt160(from));
            var (to_balance, _, _) = GetAccountState(clonedCache, new UInt160(to));

            Assert.AreEqual(99999999, from_balance);
            Assert.AreEqual(1, to_balance);

            // Check unclaim

            unclaim = Check_UnclaimedGas(clonedCache, from, persistingBlock);
            Assert.AreEqual(BigInteger.Zero, unclaim.Value);
            Assert.IsTrue(unclaim.State);

            Assert.AreEqual(keyCount + 4, clonedCache.GetChangeSet().Count()); // Gas + new balance

            // Return balance

            keyCount = clonedCache.GetChangeSet().Count();

            Assert.IsTrue(NativeContract.NEO.Transfer(clonedCache, to, from, BigInteger.One, true, persistingBlock));
            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(clonedCache, to));
            Assert.AreEqual(keyCount - 1, clonedCache.GetChangeSet().Count());  // Remove neo balance from address two

            // Bad inputs

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => NativeContract.NEO.Transfer(clonedCache, from, to, BigInteger.MinusOne, true, persistingBlock));
            Assert.ThrowsException<FormatException>(() => NativeContract.NEO.Transfer(clonedCache, new byte[19], to, BigInteger.One, false, persistingBlock));
            Assert.ThrowsException<FormatException>(() => NativeContract.NEO.Transfer(clonedCache, from, new byte[19], BigInteger.One, false, persistingBlock));

            // More than balance

            Assert.IsFalse(NativeContract.NEO.Transfer(clonedCache, to, from, new BigInteger(2), true, persistingBlock));
        }

        [TestMethod]
        public void Check_BalanceOf()
        {
            var clonedCache = _snapshotCache.CloneCache();
            byte[] account = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

            Assert.AreEqual(100_000_000, NativeContract.NEO.BalanceOf(clonedCache, account));

            account[5]++; // Without existing balance

            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(clonedCache, account));
        }

        [TestMethod]
        public void Check_CommitteeBonus()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 1,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() },
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };
            Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

            var committee = TestProtocolSettings.Default.StandbyCommittee;
            Assert.AreEqual(50000000, NativeContract.GAS.BalanceOf(clonedCache, Contract.CreateSignatureContract(committee[0]).ScriptHash));
            Assert.AreEqual(50000000, NativeContract.GAS.BalanceOf(clonedCache, Contract.CreateSignatureContract(committee[1]).ScriptHash));
            Assert.AreEqual(0, NativeContract.GAS.BalanceOf(clonedCache, Contract.CreateSignatureContract(committee[2]).ScriptHash));
        }

        [TestMethod]
        public void Check_Initialize()
        {
            var clonedCache = _snapshotCache.CloneCache();

            // StandbyValidators

            Check_GetCommittee(clonedCache, null);
        }

        [TestMethod]
        public void TestCalculateBonus()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block();

            StorageKey key = CreateStorageKey(20, UInt160.Zero.ToArray());

            // Fault: balance < 0

            clonedCache.Add(key, new StorageItem(new NeoAccountState
            {
                Balance = -100
            }));
            try
            {
                NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 10);
                Assert.Fail("Should have thrown ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException) { }
            clonedCache.Delete(key);

            // Fault range: start >= end

            clonedCache.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100,
                BalanceHeight = 100
            }));
            try
            {
                NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 10);
                Assert.Fail("Should have thrown ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException) { }
            clonedCache.Delete(key);

            // Fault range: start >= end

            clonedCache.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100,
                BalanceHeight = 100
            }));
            try
            {
                NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 10);
                Assert.Fail("Should have thrown ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException) { }
            clonedCache.Delete(key);

            // Normal 1) votee is non exist

            clonedCache.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100
            }));

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            var item = clonedCache.GetAndChange(storageKey).GetInteroperable<HashIndexState>();
            item.Index = 99;

            Assert.AreEqual(new BigInteger(0.5 * 100 * 100), NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 100));
            clonedCache.Delete(key);

            // Normal 2) votee is not committee

            clonedCache.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100,
                VoteTo = ECCurve.Secp256r1.G
            }));
            Assert.AreEqual(new BigInteger(0.5 * 100 * 100), NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 100));
            clonedCache.Delete(key);

            // Normal 3) votee is committee

            clonedCache.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100,
                VoteTo = TestProtocolSettings.Default.StandbyCommittee[0]
            }));
            clonedCache.Add(new KeyBuilder(NativeContract.NEO.Id, 23).Add(TestProtocolSettings.Default.StandbyCommittee[0]).AddBigEndian(uint.MaxValue - 50), new StorageItem() { Value = new BigInteger(50 * 10000L).ToByteArray() });
            Assert.AreEqual(new BigInteger(50 * 100), NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 100));
            clonedCache.Delete(key);
        }

        [TestMethod]
        public void TestGetNextBlockValidators1()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var result = (VM.Types.Array)NativeContract.NEO.Call(snapshotCache, "getNextBlockValidators");
            Assert.AreEqual(7, result.Count);
            Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[0].GetSpan().ToHexString());
            Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[1].GetSpan().ToHexString());
            Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[2].GetSpan().ToHexString());
            Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[3].GetSpan().ToHexString());
            Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[4].GetSpan().ToHexString());
            Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[5].GetSpan().ToHexString());
            Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[6].GetSpan().ToHexString());
        }

        [TestMethod]
        public void TestGetNextBlockValidators2()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var result = NativeContract.NEO.GetNextBlockValidators(clonedCache, 7);
            Assert.AreEqual(7, result.Length);
            Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[0].ToArray().ToHexString());
            Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[1].ToArray().ToHexString());
            Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[2].ToArray().ToHexString());
            Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[3].ToArray().ToHexString());
            Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[4].ToArray().ToHexString());
            Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[5].ToArray().ToHexString());
            Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[6].ToArray().ToHexString());
        }

        [TestMethod]
        public void TestGetCandidates1()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var array = (VM.Types.Array)NativeContract.NEO.Call(snapshotCache, "getCandidates");
            Assert.AreEqual(0, array.Count);
        }

        [TestMethod]
        public void TestGetCandidates2()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var result = NativeContract.NEO.GetCandidatesInternal(clonedCache);
            Assert.AreEqual(0, result.Count());

            StorageKey key = NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G);
            clonedCache.Add(key, new StorageItem(new CandidateState() { Registered = true }));
            Assert.AreEqual(1, NativeContract.NEO.GetCandidatesInternal(clonedCache).Count());
        }

        [TestMethod]
        public void TestCheckCandidate()
        {
            var cloneCache = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommittee(cloneCache);
            var point = committee[0].EncodePoint(true);

            // Prepare Prefix_VoterRewardPerCommittee
            var storageKey = new KeyBuilder(NativeContract.NEO.Id, 23).Add(committee[0]);
            cloneCache.Add(storageKey, new StorageItem(new BigInteger(1000)));

            // Prepare Candidate
            storageKey = new KeyBuilder(NativeContract.NEO.Id, 33).Add(committee[0]);
            cloneCache.Add(storageKey, new StorageItem(new CandidateState { Registered = true, Votes = BigInteger.One }));

            storageKey = new KeyBuilder(NativeContract.NEO.Id, 23).Add(committee[0]);
            Assert.AreEqual(1, cloneCache.Find(storageKey.ToArray()).ToArray().Length);

            // Pre-persist
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 21,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() },
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };
            Assert.IsTrue(Check_OnPersist(cloneCache, persistingBlock));

            // Clear votes
            storageKey = new KeyBuilder(NativeContract.NEO.Id, 33).Add(committee[0]);
            cloneCache.GetAndChange(storageKey).GetInteroperable<CandidateState>().Votes = BigInteger.Zero;

            // Unregister candidate, remove
            var ret = Check_UnregisterCandidate(cloneCache, point, persistingBlock);
            Assert.IsTrue(ret.State);
            Assert.IsTrue(ret.Result);

            storageKey = new KeyBuilder(NativeContract.NEO.Id, 23).Add(committee[0]);
            Assert.AreEqual(0, cloneCache.Find(storageKey.ToArray()).ToArray().Length);

            // Post-persist
            Assert.IsTrue(Check_PostPersist(cloneCache, persistingBlock));

            storageKey = new KeyBuilder(NativeContract.NEO.Id, 23).Add(committee[0]);
            Assert.AreEqual(1, cloneCache.Find(storageKey.ToArray()).ToArray().Length);
        }

        [TestMethod]
        public void TestGetCommittee()
        {
            var clonedCache = TestBlockchain.GetTestSnapshotCache();
            var result = (VM.Types.Array)NativeContract.NEO.Call(clonedCache, "getCommittee");
            Assert.AreEqual(21, result.Count);
            Assert.AreEqual("020f2887f41474cfeb11fd262e982051c1541418137c02a0f4961af911045de639", result[0].GetSpan().ToHexString());
            Assert.AreEqual("03204223f8c86b8cd5c89ef12e4f0dbb314172e9241e30c9ef2293790793537cf0", result[1].GetSpan().ToHexString());
            Assert.AreEqual("0222038884bbd1d8ff109ed3bdef3542e768eef76c1247aea8bc8171f532928c30", result[2].GetSpan().ToHexString());
            Assert.AreEqual("0226933336f1b75baa42d42b71d9091508b638046d19abd67f4e119bf64a7cfb4d", result[3].GetSpan().ToHexString());
            Assert.AreEqual("023a36c72844610b4d34d1968662424011bf783ca9d984efa19a20babf5582f3fe", result[4].GetSpan().ToHexString());
            Assert.AreEqual("03409f31f0d66bdc2f70a9730b66fe186658f84a8018204db01c106edc36553cd0", result[5].GetSpan().ToHexString());
            Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[6].GetSpan().ToHexString());
            Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[7].GetSpan().ToHexString());
            Assert.AreEqual("02504acbc1f4b3bdad1d86d6e1a08603771db135a73e61c9d565ae06a1938cd2ad", result[8].GetSpan().ToHexString());
            Assert.AreEqual("03708b860c1de5d87f5b151a12c2a99feebd2e8b315ee8e7cf8aa19692a9e18379", result[9].GetSpan().ToHexString());
            Assert.AreEqual("0288342b141c30dc8ffcde0204929bb46aed5756b41ef4a56778d15ada8f0c6654", result[10].GetSpan().ToHexString());
            Assert.AreEqual("02a62c915cf19c7f19a50ec217e79fac2439bbaad658493de0c7d8ffa92ab0aa62", result[11].GetSpan().ToHexString());
            Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[12].GetSpan().ToHexString());
            Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[13].GetSpan().ToHexString());
            Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[14].GetSpan().ToHexString());
            Assert.AreEqual("03c6aa6e12638b36e88adc1ccdceac4db9929575c3e03576c617c49cce7114a050", result[15].GetSpan().ToHexString());
            Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[16].GetSpan().ToHexString());
            Assert.AreEqual("02cd5a5547119e24feaa7c2a0f37b8c9366216bab7054de0065c9be42084003c8a", result[17].GetSpan().ToHexString());
            Assert.AreEqual("03cdcea66032b82f5c30450e381e5295cae85c5e6943af716cc6b646352a6067dc", result[18].GetSpan().ToHexString());
            Assert.AreEqual("03d281b42002647f0113f36c7b8efb30db66078dfaaa9ab3ff76d043a98d512fde", result[19].GetSpan().ToHexString());
            Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[20].GetSpan().ToHexString());
        }

        [TestMethod]
        public void TestGetValidators()
        {
            var clonedCache = _snapshotCache.CloneCache();
            var result = NativeContract.NEO.ComputeNextBlockValidators(clonedCache, TestProtocolSettings.Default);
            Assert.AreEqual("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", result[0].ToArray().ToHexString());
            Assert.AreEqual("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", result[1].ToArray().ToHexString());
            Assert.AreEqual("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", result[2].ToArray().ToHexString());
            Assert.AreEqual("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", result[3].ToArray().ToHexString());
            Assert.AreEqual("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", result[4].ToArray().ToHexString());
            Assert.AreEqual("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", result[5].ToArray().ToHexString());
            Assert.AreEqual("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", result[6].ToArray().ToHexString());
        }

        [TestMethod]
        public void TestOnBalanceChanging()
        {
            var ret = Transfer4TesingOnBalanceChanging(new BigInteger(0), false);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);

            ret = Transfer4TesingOnBalanceChanging(new BigInteger(1), false);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);

            ret = Transfer4TesingOnBalanceChanging(new BigInteger(1), true);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
        }

        [TestMethod]
        public void TestTotalSupply()
        {
            var clonedCache = _snapshotCache.CloneCache();
            Assert.AreEqual(new BigInteger(100000000), NativeContract.NEO.TotalSupply(clonedCache));
        }

        [TestMethod]
        public void TestEconomicParameter()
        {
            const byte Prefix_CurrentBlock = 12;
            var clonedCache = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header() };

            (BigInteger, bool) result = Check_GetGasPerBlock(clonedCache, persistingBlock);
            Assert.IsTrue(result.Item2);
            Assert.AreEqual(5 * NativeContract.GAS.Factor, result.Item1);

            persistingBlock = new Block { Header = new Header { Index = 10 } };
            (Boolean, bool) result1 = Check_SetGasPerBlock(clonedCache, 10 * NativeContract.GAS.Factor, persistingBlock);
            Assert.IsTrue(result1.Item2);
            Assert.IsTrue(result1.Item1.GetBoolean());

            var height = clonedCache[NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>();
            height.Index = persistingBlock.Index + 1;
            result = Check_GetGasPerBlock(clonedCache, persistingBlock);
            Assert.IsTrue(result.Item2);
            Assert.AreEqual(10 * NativeContract.GAS.Factor, result.Item1);

            // Check calculate bonus
            StorageItem storage = clonedCache.GetOrAdd(CreateStorageKey(20, UInt160.Zero.ToArray()), () => new StorageItem(new NeoAccountState()));
            NeoAccountState state = storage.GetInteroperable<NeoAccountState>();
            state.Balance = 1000;
            state.BalanceHeight = 0;
            height.Index = persistingBlock.Index + 1;
            Assert.AreEqual(6500, NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, persistingBlock.Index + 2));
        }

        [TestMethod]
        public void TestClaimGas()
        {
            var clonedCache = _snapshotCache.CloneCache();

            // Initialize block
            clonedCache.Add(CreateStorageKey(1), new StorageItem(new BigInteger(30000000)));

            ECPoint[] standbyCommittee = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray();
            CachedCommittee cachedCommittee = new();
            for (var i = 0; i < TestProtocolSettings.Default.CommitteeMembersCount; i++)
            {
                ECPoint member = standbyCommittee[i];
                clonedCache.Add(new KeyBuilder(NativeContract.NEO.Id, 33).Add(member), new StorageItem(new CandidateState()
                {
                    Registered = true,
                    Votes = 200 * 10000
                }));
                cachedCommittee.Add((member, 200 * 10000));
            }
            clonedCache.GetOrAdd(new KeyBuilder(NativeContract.NEO.Id, 14), () => new StorageItem()).Value = BinarySerializer.Serialize(cachedCommittee.ToStackItem(null), ExecutionEngineLimits.Default);

            var item = clonedCache.GetAndChange(new KeyBuilder(NativeContract.NEO.Id, 1), () => new StorageItem());
            item.Value = ((BigInteger)2100 * 10000L).ToByteArray();

            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 0,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() },
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };
            Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

            var committee = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray();
            var accountA = committee[0];
            var accountB = committee[TestProtocolSettings.Default.CommitteeMembersCount - 1];
            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(clonedCache, Contract.CreateSignatureContract(accountA).ScriptHash));

            StorageItem storageItem = clonedCache.TryGet(new KeyBuilder(NativeContract.NEO.Id, 23).Add(accountA));
            Assert.AreEqual(30000000000, (BigInteger)storageItem);

            Assert.IsNull(clonedCache.TryGet(new KeyBuilder(NativeContract.NEO.Id, 23).Add(accountB).AddBigEndian(uint.MaxValue - 1)));

            // Next block

            persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 1,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() },
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };
            Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(clonedCache, Contract.CreateSignatureContract(committee[1]).ScriptHash));

            storageItem = clonedCache.TryGet(new KeyBuilder(NativeContract.NEO.Id, 23).Add(committee[1]));
            Assert.AreEqual(30000000000, (BigInteger)storageItem);

            // Next block

            persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 21,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() },
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };
            Assert.IsTrue(Check_PostPersist(clonedCache, persistingBlock));

            accountA = TestProtocolSettings.Default.StandbyCommittee.OrderBy(p => p).ToArray()[2];
            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(clonedCache, Contract.CreateSignatureContract(committee[2]).ScriptHash));

            storageItem = clonedCache.TryGet(new KeyBuilder(NativeContract.NEO.Id, 23).Add(committee[2]));
            Assert.AreEqual(30000000000 * 2, (BigInteger)storageItem);

            // Claim GAS

            var account = Contract.CreateSignatureContract(committee[2]).ScriptHash;
            clonedCache.Add(new KeyBuilder(NativeContract.NEO.Id, 20).Add(account), new StorageItem(new NeoAccountState
            {
                BalanceHeight = 3,
                Balance = 200 * 10000 - 2 * 100,
                VoteTo = committee[2],
                LastGasPerVote = 30000000000,
            }));
            Assert.AreEqual(1999800, NativeContract.NEO.BalanceOf(clonedCache, account));
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            clonedCache.GetAndChange(storageKey).GetInteroperable<HashIndexState>().Index = 29 + 2;
            BigInteger value = NativeContract.NEO.UnclaimedGas(clonedCache, account, 29 + 3);
            Assert.AreEqual(1999800 * 30000000000 / 100000000L + (1999800L * 10 * 5 * 29 / 100), value);
        }

        [TestMethod]
        public void TestUnclaimedGas()
        {
            var clonedCache = _snapshotCache.CloneCache();
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 10));
            clonedCache.Add(CreateStorageKey(20, UInt160.Zero.ToArray()), new StorageItem(new NeoAccountState()));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(clonedCache, UInt160.Zero, 10));
        }

        [TestMethod]
        public void TestVote()
        {
            var clonedCache = _snapshotCache.CloneCache();
            UInt160 account = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
            StorageKey keyAccount = CreateStorageKey(20, account.ToArray());
            StorageKey keyValidator = CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray());
            _persistingBlock.Header.Index = 1;
            var ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), false, _persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsTrue(ret.State);

            ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true, _persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsTrue(ret.State);

            clonedCache.Add(keyAccount, new StorageItem(new NeoAccountState()));
            ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true, _persistingBlock);
            Assert.IsFalse(ret.Result);
            Assert.IsTrue(ret.State);

            var (_, _, vote_to_null) = GetAccountState(clonedCache, account);
            Assert.IsNull(vote_to_null);

            clonedCache.Delete(keyAccount);
            clonedCache.GetAndChange(keyAccount, () => new StorageItem(new NeoAccountState
            {
                Balance = 1,
                VoteTo = ECCurve.Secp256r1.G
            }));
            clonedCache.Add(keyValidator, new StorageItem(new CandidateState() { Registered = true }));
            ret = Check_Vote(clonedCache, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true, _persistingBlock);
            Assert.IsTrue(ret.Result);
            Assert.IsTrue(ret.State);
            var (_, _, voteto) = GetAccountState(clonedCache, account);
            Assert.AreEqual(ECCurve.Secp256r1.G.ToArray().ToHexString(), voteto.ToHexString());
        }

        internal (bool State, bool Result) Transfer4TesingOnBalanceChanging(BigInteger amount, bool addVotes)
        {
            var clonedCache = _snapshotCache.CloneCache();
            _persistingBlock.Header.Index = 1;
            var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), clonedCache, _persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            ScriptBuilder sb = new();
            var tmp = engine.ScriptContainer.GetScriptHashesForVerifying(engine.SnapshotCache);
            UInt160 from = engine.ScriptContainer.GetScriptHashesForVerifying(engine.SnapshotCache)[0];
            if (addVotes)
            {
                clonedCache.Add(CreateStorageKey(20, from.ToArray()), new StorageItem(new NeoAccountState
                {
                    VoteTo = ECCurve.Secp256r1.G,
                    Balance = new BigInteger(1000)
                }));
                clonedCache.Add(NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G), new StorageItem(new CandidateState()));
            }
            else
            {
                clonedCache.Add(CreateStorageKey(20, from.ToArray()), new StorageItem(new NeoAccountState
                {
                    Balance = new BigInteger(1000)
                }));
            }

            sb.EmitDynamicCall(NativeContract.NEO.Hash, "transfer", from, UInt160.Zero, amount, null);
            engine.LoadScript(sb.ToArray());
            var state = engine.Execute();
            Console.WriteLine($"{state} {engine.FaultException}");
            var result = engine.ResultStack.Peek();
            Assert.AreEqual(typeof(Boolean), result.GetType());
            return (true, result.GetBoolean());
        }

        internal static bool Check_OnPersist(DataCache clonedCache, Block persistingBlock)
        {
            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            return engine.Execute() == VMState.HALT;
        }

        internal static bool Check_PostPersist(DataCache clonedCache, Block persistingBlock)
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
            using var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            return engine.Execute() == VMState.HALT;
        }

        internal static (BigInteger Value, bool State) Check_GetGasPerBlock(DataCache clonedCache, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "getGasPerBlock");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Integer));

            return (((Integer)result).GetInteger(), true);
        }

        internal static (Boolean Value, bool State) Check_SetGasPerBlock(DataCache clonedCache, BigInteger gasPerBlock, Block persistingBlock)
        {
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(clonedCache);
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "setGasPerBlock", gasPerBlock);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
                return (false, false);

            return (true, true);
        }

        internal static (bool State, bool Result) Check_Vote(DataCache clonedCache, byte[] account, byte[] pubkey, bool signAccount, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "vote", account, pubkey);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                Console.WriteLine(engine.FaultException);
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Boolean));

            return (true, result.GetBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterValidator(DataCache clonedCache, byte[] pubkey, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "registerCandidate", pubkey);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Boolean));

            return (true, result.GetBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterValidatorViaNEP27(DataCache clonedCache, ECPoint pubkey, Block persistingBlock, bool passNEO, byte[] data, BigInteger amount)
        {
            var keyScriptHash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            var contractID = passNEO ? NativeContract.NEO.Id : NativeContract.GAS.Id;
            var storageKey = new KeyBuilder(contractID, 20).Add(keyScriptHash); // 20 is Prefix_Account

            if (passNEO)
                clonedCache.Add(storageKey, new StorageItem(new NeoAccountState { Balance = amount }));
            else
                clonedCache.Add(storageKey, new StorageItem(new AccountState { Balance = amount }));

            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(keyScriptHash), clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1_0000_0000);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(passNEO ? NativeContract.NEO.Hash : NativeContract.GAS.Hash, "transfer", keyScriptHash, NativeContract.NEO.Hash, amount, data);
            engine.LoadScript(script.ToArray());

            var execRes = engine.Execute();
            clonedCache.Delete(storageKey); // Clean up for subsequent invocations.

            if (execRes == VMState.FAULT)
                return (false, false);

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Boolean));

            return (true, result.GetBoolean());
        }

        internal static ECPoint[] Check_GetCommittee(DataCache clonedCache, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "getCommittee");
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(VM.Types.Array));

            return (result as VM.Types.Array).Select(u => ECPoint.DecodePoint(u.GetSpan(), ECCurve.Secp256r1)).ToArray();
        }

        internal static (BigInteger Value, bool State) Check_UnclaimedGas(DataCache clonedCache, byte[] address, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "unclaimedGas", address, persistingBlock.Index);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                Console.WriteLine(engine.FaultException);
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Integer));

            return (result.GetInteger(), true);
        }

        internal static void CheckValidator(ECPoint eCPoint, DataCache.Trackable trackable)
        {
            BigInteger st = trackable.Item;
            Assert.AreEqual(0, st);

            CollectionAssert.AreEqual(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)).ToArray(), trackable.Key.Key.ToArray());
        }

        internal static void CheckBalance(byte[] account, DataCache.Trackable trackable, BigInteger balance, BigInteger height, ECPoint voteTo)
        {
            var st = (Struct)BinarySerializer.Deserialize(trackable.Item.Value, ExecutionEngineLimits.Default);

            Assert.AreEqual(3, st.Count);
            CollectionAssert.AreEqual(new Type[] { typeof(Integer), typeof(Integer), typeof(ByteString) }, st.Select(u => u.GetType()).ToArray()); // Balance

            Assert.AreEqual(balance, st[0].GetInteger()); // Balance
            Assert.AreEqual(height, st[1].GetInteger());  // BalanceHeight
            Assert.AreEqual(voteTo, ECPoint.DecodePoint(st[2].GetSpan(), ECCurve.Secp256r1));  // Votes

            CollectionAssert.AreEqual(new byte[] { 20 }.Concat(account).ToArray(), trackable.Key.Key.ToArray());
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = NativeContract.NEO.Id,
                Key = buffer
            };
        }

        internal static (bool State, bool Result) Check_UnregisterCandidate(DataCache clonedCache, byte[] pubkey, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), clonedCache, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "unregisterCandidate", pubkey);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Boolean));

            return (true, result.GetBoolean());
        }

        internal static (BigInteger balance, BigInteger height, byte[] voteto) GetAccountState(DataCache clonedCache, UInt160 account)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, clonedCache, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "getAccountState", account);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());

            var result = engine.ResultStack.Pop();
            Assert.IsInstanceOfType(result, typeof(Struct));

            Struct state = (result as Struct);
            var balance = state[0].GetInteger();
            var height = state[1].GetInteger();
            var voteto = state[2].IsNull ? null : state[2].GetSpan().ToArray();
            return (balance, height, voteto);
        }
    }
}
