using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Linq;
using System.Numerics;
using static Neo.SmartContract.Native.Tokens.NeoToken;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_NeoToken
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Check_Name() => NativeContract.NEO.Name().Should().Be("NEO");

        [TestMethod]
        public void Check_Symbol() => NativeContract.NEO.Symbol().Should().Be("neo");

        [TestMethod]
        public void Check_Decimals() => NativeContract.NEO.Decimals().Should().Be(0);

        [TestMethod]
        public void Check_Vote()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            // No signature

            var ret = Check_Vote(snapshot, from, null, false);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();

            // Wrong address

            ret = Check_Vote(snapshot, new byte[19], null, false);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeFalse();

            // Wrong ec

            ret = Check_Vote(snapshot, from, new byte[19], true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeFalse();

            // no registered

            var fakeAddr = new byte[20];
            fakeAddr[0] = 0x5F;
            fakeAddr[5] = 0xFF;

            ret = Check_Vote(snapshot, fakeAddr, null, true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();

            // no registered

            var accountState = snapshot.Storages.TryGet(CreateStorageKey(20, from)).GetInteroperable<NeoAccountState>();
            accountState.VoteTo = null;
            ret = Check_Vote(snapshot, from, ECCurve.Secp256r1.G.ToArray(), true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();
            accountState.VoteTo.Should().BeNull();

            // normal case

            snapshot.Storages.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState()));
            ret = Check_Vote(snapshot, from, ECCurve.Secp256r1.G.ToArray(), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            accountState.VoteTo.Should().Be(ECCurve.Secp256r1.G);
        }

        [TestMethod]
        public void Check_Vote_Sameaccounts()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();
            var accountState = snapshot.Storages.TryGet(CreateStorageKey(20, from)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            snapshot.Storages.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState()));
            var ret = Check_Vote(snapshot, from, ECCurve.Secp256r1.G.ToArray(), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            accountState.VoteTo.Should().Be(ECCurve.Secp256r1.G);

            //two account vote for the same account
            var stateValidator = snapshot.Storages.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            stateValidator.Votes.Should().Be(100);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState { Balance = 200 }));
            var secondAccount = snapshot.Storages.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            ret = Check_Vote(snapshot, G_Account, ECCurve.Secp256r1.G.ToArray(), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            stateValidator.Votes.Should().Be(300);
        }

        [TestMethod]
        public void Check_Vote_ChangeVote()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };
            //from vote to G
            byte[] from = Blockchain.StandbyValidators[0].ToArray();
            var from_Account = Contract.CreateSignatureContract(Blockchain.StandbyValidators[0]).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, from_Account), new StorageItem(new NeoAccountState()));
            var accountState = snapshot.Storages.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            snapshot.Storages.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState()));
            var ret = Check_Vote(snapshot, from_Account, ECCurve.Secp256r1.G.ToArray(), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            accountState.VoteTo.Should().Be(ECCurve.Secp256r1.G);

            //from change vote to itself
            var G_stateValidator = snapshot.Storages.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            G_stateValidator.Votes.Should().Be(100);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState { Balance = 200 }));
            snapshot.Storages.Add(CreateStorageKey(33, from), new StorageItem(new CandidateState()));
            ret = Check_Vote(snapshot, from_Account, from, true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            G_stateValidator.Votes.Should().Be(0);
            var from_stateValidator = snapshot.Storages.GetAndChange(CreateStorageKey(33, from)).GetInteroperable<CandidateState>();
            from_stateValidator.Votes.Should().Be(100);
        }

        [TestMethod]
        public void Check_Vote_VoteToNull()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Blockchain.StandbyValidators[0].ToArray();
            var from_Account = Contract.CreateSignatureContract(Blockchain.StandbyValidators[0]).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, from_Account), new StorageItem(new NeoAccountState()));
            var accountState = snapshot.Storages.TryGet(CreateStorageKey(20, from_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            snapshot.Storages.Add(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray()), new StorageItem(new CandidateState()));
            var ret = Check_Vote(snapshot, from_Account, ECCurve.Secp256r1.G.ToArray(), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            accountState.VoteTo.Should().Be(ECCurve.Secp256r1.G);

            //from vote to null account G votes becomes 0
            var G_stateValidator = snapshot.Storages.GetAndChange(CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray())).GetInteroperable<CandidateState>();
            G_stateValidator.Votes.Should().Be(100);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState { Balance = 200 }));
            snapshot.Storages.Add(CreateStorageKey(33, from), new StorageItem(new CandidateState()));
            ret = Check_Vote(snapshot, from_Account, null, true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
            G_stateValidator.Votes.Should().Be(0);
            accountState.VoteTo.Should().Be(null);
        }

        [TestMethod]
        public void Check_UnclaimedGas()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0.5 * 1000 * 100000000L));
            unclaim.State.Should().BeTrue();

            unclaim = Check_UnclaimedGas(snapshot, new byte[19]);
            unclaim.Value.Should().Be(BigInteger.Zero);
            unclaim.State.Should().BeFalse();
        }

        [TestMethod]
        public void Check_RegisterValidator()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var point = Blockchain.StandbyValidators[0].EncodePoint(true);

            var ret = Check_RegisterValidator(snapshot, point); // Exists
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(++keyCount); // No changes

            point[20]++; // fake point
            ret = Check_RegisterValidator(snapshot, point); // New

            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1); // New validator

            // Check GetRegisteredValidators

            var members = NativeContract.NEO.GetCandidates(snapshot);
            Assert.AreEqual(2, members.Length);
        }

        [TestMethod]
        public void Check_UnregisterCandidate()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var point = Blockchain.StandbyValidators[0].EncodePoint(true);

            //without register
            var ret = Check_UnregisterCandidate(snapshot, point);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount);

            //register and then unregister
            ret = Check_RegisterValidator(snapshot, point);
            StorageItem item = snapshot.Storages.GetAndChange(CreateStorageKey(33, point));
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            var members = NativeContract.NEO.GetCandidates(snapshot);
            Assert.AreEqual(1, members.Length);
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1);
            StorageKey key = CreateStorageKey(33, point);
            snapshot.Storages.TryGet(key).Should().NotBeNull();

            ret = Check_UnregisterCandidate(snapshot, point);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount);

            members = NativeContract.NEO.GetCandidates(snapshot);
            Assert.AreEqual(0, members.Length);
            snapshot.Storages.TryGet(key).Should().BeNull();

            //register with votes, then unregister
            ret = Check_RegisterValidator(snapshot, point);
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState()));
            var accountState = snapshot.Storages.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 100;
            Check_Vote(snapshot, G_Account, Blockchain.StandbyValidators[0].ToArray(), true);
            ret = Check_UnregisterCandidate(snapshot, point);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
            snapshot.Storages.TryGet(key).Should().NotBeNull();
            StorageItem pointItem = snapshot.Storages.TryGet(key);
            CandidateState pointState = pointItem.GetInteroperable<CandidateState>();
            pointState.Registered.Should().BeFalse();
            pointState.Votes.Should().Be(100);

            //vote fail
            ret = Check_Vote(snapshot, G_Account, Blockchain.StandbyValidators[0].ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();
            accountState.VoteTo.Should().Be(Blockchain.StandbyValidators[0]);
        }

        [TestMethod]
        public void Check_GetCommittee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var point = Blockchain.StandbyValidators[0].EncodePoint(true);

            //register with votes with 20000000
            var G_Account = Contract.CreateSignatureContract(ECCurve.Secp256r1.G).ScriptHash.ToArray();
            snapshot.Storages.Add(CreateStorageKey(20, G_Account), new StorageItem(new NeoAccountState()));
            var accountState = snapshot.Storages.TryGet(CreateStorageKey(20, G_Account)).GetInteroperable<NeoAccountState>();
            accountState.Balance = 20000000;
            var ret = Check_RegisterValidator(snapshot, ECCurve.Secp256r1.G.ToArray());
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
            ret = Check_Vote(snapshot, G_Account, ECCurve.Secp256r1.G.ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            var committeemembers = NativeContract.NEO.GetCommittee(snapshot);
            var defaultCommittee = Blockchain.StandbyCommittee.OrderBy(p => p).ToArray();
            committeemembers.GetType().Should().Be(typeof(ECPoint[]));
            for (int i = 0; i < ProtocolSettings.Default.CommitteeMembersCount; i++)
            {
                committeemembers[i].Should().Be(defaultCommittee[i]);
            }

            //register more candidates,committee member change
            for (int i = 0; i < ProtocolSettings.Default.CommitteeMembersCount - 1; i++)
            {
                Check_RegisterValidator(snapshot, Blockchain.StandbyCommittee[i].ToArray());
                var currentCandidates = NativeContract.NEO.GetCandidates(snapshot);
            }
            committeemembers = NativeContract.NEO.GetCommittee(snapshot);
            committeemembers.Length.Should().Be(ProtocolSettings.Default.CommitteeMembersCount);
            committeemembers.Contains(ECCurve.Secp256r1.G).Should().BeTrue();
            for (int i = 0; i < ProtocolSettings.Default.CommitteeMembersCount - 1; i++)
            {
                committeemembers.Contains(Blockchain.StandbyCommittee[i]).Should().BeTrue();
            }
            committeemembers.Contains(Blockchain.StandbyCommittee[ProtocolSettings.Default.CommitteeMembersCount - 1]).Should().BeFalse();
        }

        [TestMethod]
        public void Check_Transfer()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            byte[] to = new byte[20];

            var keyCount = snapshot.Storages.GetChangeSet().Count();

            // Check unclaim

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0.5 * 1000 * 100000000L));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.One, false).Should().BeFalse(); // Not signed
            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.One, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(99999999);
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(1);

            // Check unclaim

            unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 4); // Gas + new balance

            // Return balance

            keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.NEO.Transfer(snapshot, to, from, BigInteger.One, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(0);
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount - 1);  // Remove neo balance from address two

            // Bad inputs

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.MinusOne, true).Should().BeFalse();
            NativeContract.NEO.Transfer(snapshot, new byte[19], to, BigInteger.One, false).Should().BeFalse();
            NativeContract.NEO.Transfer(snapshot, from, new byte[19], BigInteger.One, false).Should().BeFalse();

            // More than balance

            NativeContract.NEO.Transfer(snapshot, to, from, new BigInteger(2), true).Should().BeFalse();
        }

        [TestMethod]
        public void Check_BalanceOf()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            byte[] account = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            NativeContract.NEO.BalanceOf(snapshot, account).Should().Be(100_000_000);

            account[5]++; // Without existing balance

            NativeContract.NEO.BalanceOf(snapshot, account).Should().Be(0);
        }

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // StandbyValidators

            Check_GetValidators(snapshot);
        }

        [TestMethod]
        public void Check_BadScript()
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, Blockchain.Singleton.GetSnapshot(), 0);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            Assert.ThrowsException<InvalidOperationException>(() => NativeContract.NEO.Invoke(engine));
        }

        [TestMethod]
        public void TestCalculateBonus()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            StorageKey key = CreateStorageKey(20, UInt160.Zero.ToArray());

            // Fault: balance < 0

            snapshot.Storages.Add(key, new StorageItem(new NeoAccountState
            {
                Balance = -100
            }));
            Action action = () => NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            action.Should().Throw<ArgumentOutOfRangeException>();
            snapshot.Storages.Delete(key);

            // Fault range: start >= end

            snapshot.Storages.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100,
                BalanceHeight = 100
            }));
            action = () => NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            snapshot.Storages.Delete(key);

            // Normal 1) votee is non exist

            snapshot.Storages.GetAndChange(key, () => new StorageItem(new NeoAccountState
            {
                Balance = 100
            }));
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 100).Should().Be(new BigInteger(0.5 * 100 * 100));
            snapshot.Storages.Delete(key);
        }

        [TestMethod]
        public void TestGetNextBlockValidators1()
        {
            using (ApplicationEngine engine = NativeContract.NEO.TestCall("getNextBlockValidators"))
            {
                var result = engine.ResultStack.Peek();
                result.GetType().Should().Be(typeof(VM.Types.Array));
                ((VM.Types.Array)result).Count.Should().Be(7);
                ((VM.Types.Array)result)[0].GetSpan().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
                ((VM.Types.Array)result)[1].GetSpan().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
                ((VM.Types.Array)result)[2].GetSpan().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
                ((VM.Types.Array)result)[3].GetSpan().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
                ((VM.Types.Array)result)[4].GetSpan().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
                ((VM.Types.Array)result)[5].GetSpan().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
                ((VM.Types.Array)result)[6].GetSpan().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
            }
        }

        [TestMethod]
        public void TestGetNextBlockValidators2()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var result = NativeContract.NEO.GetNextBlockValidators(snapshot);
            result.Length.Should().Be(7);
            result[0].ToArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
            result[1].ToArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
            result[2].ToArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
            result[3].ToArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
            result[4].ToArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
            result[5].ToArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
            result[6].ToArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");

            snapshot.Storages.Add(CreateStorageKey(14), new StorageItem()
            {
                Value = new ECPoint[] { ECCurve.Secp256r1.G }.ToByteArray()
            });
            result = NativeContract.NEO.GetNextBlockValidators(snapshot);
            result.Length.Should().Be(1);
            result[0].ToArray().ToHexString().Should().Be("036b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296");
        }

        [TestMethod]
        public void TestGetCandidates1()
        {
            using ApplicationEngine engine = NativeContract.NEO.TestCall("getCandidates");
            var array = engine.ResultStack.Pop<VM.Types.Array>();
            array.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestGetCandidates2()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var result = NativeContract.NEO.GetCandidates(snapshot);
            result.Length.Should().Be(0);

            StorageKey key = NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G);
            snapshot.Storages.Add(key, new StorageItem(new CandidateState()));
            NativeContract.NEO.GetCandidates(snapshot).Length.Should().Be(1);
        }

        [TestMethod]
        public void TestGetValidators1()
        {
            using (ApplicationEngine engine = NativeContract.NEO.TestCall("getValidators"))
            {
                var result = engine.ResultStack.Peek();
                result.GetType().Should().Be(typeof(VM.Types.Array));
                ((VM.Types.Array)result).Count.Should().Be(7);
                ((VM.Types.Array)result)[0].GetSpan().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
                ((VM.Types.Array)result)[1].GetSpan().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
                ((VM.Types.Array)result)[2].GetSpan().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
                ((VM.Types.Array)result)[3].GetSpan().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
                ((VM.Types.Array)result)[4].GetSpan().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
                ((VM.Types.Array)result)[5].GetSpan().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
                ((VM.Types.Array)result)[6].GetSpan().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
            }
        }

        [TestMethod]
        public void TestGetValidators2()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var result = NativeContract.NEO.GetValidators(snapshot);
            result[0].ToArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
            result[1].ToArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
            result[2].ToArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
            result[3].ToArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
            result[4].ToArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
            result[5].ToArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
            result[6].ToArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
        }

        [TestMethod]
        public void TestOnBalanceChanging()
        {
            var ret = Transfer4TesingOnBalanceChanging(new BigInteger(0), false);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            ret = Transfer4TesingOnBalanceChanging(new BigInteger(1), false);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();

            ret = Transfer4TesingOnBalanceChanging(new BigInteger(1), true);
            ret.Result.Should().BeTrue();
            ret.State.Should().BeTrue();
        }

        [TestMethod]
        public void TestTotalSupply()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            NativeContract.NEO.TotalSupply(snapshot).Should().Be(new BigInteger(100000000));
        }

        [TestMethod]
        public void TestEconomicParameter()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            (BigInteger, bool) result = Check_GetGasPerBlock(snapshot);
            result.Item2.Should().BeTrue();
            result.Item1.Should().Be(5 * NativeContract.GAS.Factor);
        }

        [TestMethod]
        public void TestUnclaimedGas()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            snapshot.Storages.Add(CreateStorageKey(20, UInt160.Zero.ToArray()), new StorageItem(new NeoAccountState()));
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
        }

        [TestMethod]
        public void TestVote()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            UInt160 account = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
            StorageKey keyAccount = CreateStorageKey(20, account.ToArray());
            StorageKey keyValidator = CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray());
            var ret = Check_Vote(snapshot, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), false);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            ret = Check_Vote(snapshot, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.Add(keyAccount, new StorageItem(new NeoAccountState()));
            ret = Check_Vote(snapshot, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.Delete(keyAccount);
            snapshot.Storages.GetAndChange(keyAccount, () => new StorageItem(new NeoAccountState
            {
                VoteTo = ECCurve.Secp256r1.G
            }));
            snapshot.Storages.Add(keyValidator, new StorageItem(new CandidateState()));
            ret = Check_Vote(snapshot, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
        }

        internal (bool State, bool Result) Transfer4TesingOnBalanceChanging(BigInteger amount, bool addVotes)
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = Blockchain.GenesisBlock;
            var engine = ApplicationEngine.Create(TriggerType.Application, Blockchain.GenesisBlock, snapshot);
            ScriptBuilder sb = new ScriptBuilder();
            var tmp = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot);
            UInt160 from = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot)[0];
            if (addVotes)
            {
                snapshot.Storages.Add(CreateStorageKey(20, from.ToArray()), new StorageItem(new NeoAccountState
                {
                    VoteTo = ECCurve.Secp256r1.G,
                    Balance = new BigInteger(1000)
                }));
                snapshot.Storages.Add(NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G), new StorageItem(new CandidateState()));
            }
            else
            {
                snapshot.Storages.Add(CreateStorageKey(20, from.ToArray()), new StorageItem(new NeoAccountState
                {
                    Balance = new BigInteger(1000)
                }));
            }

            sb.EmitAppCall(NativeContract.NEO.Hash, "transfer", from, UInt160.Zero, amount);
            engine.LoadScript(sb.ToArray());
            engine.Execute();
            var result = engine.ResultStack.Peek();
            result.GetType().Should().Be(typeof(VM.Types.Boolean));
            return (true, result.GetBoolean());
        }

        internal static (BigInteger Value, bool State) Check_GetGasPerBlock(StoreView snapshot)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getGasPerBlock");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return (((VM.Types.Integer)result).GetInteger(), true);
        }

        internal static (bool State, bool Result) Check_Vote(StoreView snapshot, byte[] account, byte[] pubkey, bool signAccount)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();

            if (pubkey is null)
                script.Emit(OpCode.PUSHNULL);
            else
                script.EmitPush(pubkey);
            script.EmitPush(account);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("vote");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.GetBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterValidator(StoreView snapshot, byte[] pubkey)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), snapshot);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(pubkey);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("registerCandidate");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.GetBoolean());
        }

        internal static ECPoint[] Check_GetValidators(StoreView snapshot)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getValidators");
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));

            return (result as VM.Types.Array).Select(u => u.GetSpan().AsSerializable<ECPoint>()).ToArray();
        }

        internal static (BigInteger Value, bool State) Check_UnclaimedGas(StoreView snapshot, byte[] address)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(snapshot.PersistingBlock.Index);
            script.EmitPush(address);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("unclaimedGas");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return (result.GetInteger(), true);
        }

        internal static void CheckValidator(ECPoint eCPoint, DataCache<StorageKey, StorageItem>.Trackable trackable)
        {
            var st = new BigInteger(trackable.Item.Value);
            st.Should().Be(0);

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)));
            trackable.Item.IsConstant.Should().Be(false);
        }

        internal static void CheckBalance(byte[] account, DataCache<StorageKey, StorageItem>.Trackable trackable, BigInteger balance, BigInteger height, ECPoint voteTo)
        {
            var st = (VM.Types.Struct)BinarySerializer.Deserialize(trackable.Item.Value, 16, 32);

            st.Count.Should().Be(3);
            st.Select(u => u.GetType()).ToArray().Should().BeEquivalentTo(new Type[] { typeof(VM.Types.Integer), typeof(VM.Types.Integer), typeof(VM.Types.ByteString) }); // Balance

            st[0].GetInteger().Should().Be(balance); // Balance
            st[1].GetInteger().Should().Be(height);  // BalanceHeight
            st[2].GetSpan().AsSerializable<ECPoint>().Should().BeEquivalentTo(voteTo);  // Votes

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 20 }.Concat(account));
            trackable.Item.IsConstant.Should().Be(false);
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = NativeContract.NEO.Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }

        internal static (bool State, bool Result) Check_UnregisterCandidate(StoreView snapshot, byte[] pubkey)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), snapshot);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(pubkey);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("unregisterCandidate");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, result.GetBoolean());
        }
    }
}
