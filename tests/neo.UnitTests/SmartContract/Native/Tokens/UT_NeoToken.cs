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
using Neo.SmartContract.Native.Tokens;
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
        public void Check_SupportedStandards() => NativeContract.NEO.SupportedStandards().Should().BeEquivalentTo(new string[] { "NEP-5", "NEP-10" });

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

            // TODO: More votes tests
        }

        [TestMethod]
        public void Check_UnclaimedGas()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Blockchain.GetConsensusAddress(Blockchain.StandbyValidators).ToArray();

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(25000004000));
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

            var members = NativeContract.NEO.GetCandidates(snapshot).OrderBy(u => u.PublicKey).ToArray();
            var check = Blockchain.StandbyCommittee.Select(u => u.EncodePoint(true)).ToList();
            check.Add(point); // Add the new member

            for (int x = 0; x < members.Length; x++)
            {
                Assert.AreEqual(1, check.RemoveAll(u => u.SequenceEqual(members[x].PublicKey.EncodePoint(true))));
            }

            Assert.AreEqual(0, check.Count);
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
            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(3000000000000000);
            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(25000004000));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.One, false).Should().BeFalse(); // Not signed
            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.One, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(50000007);
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(1);
            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(3000025000004000);

            // Check unclaim
            snapshot.PersistingBlock = new Block() { Index = 2000 };
            unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(25000003500));
            unclaim.State.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 4); // Gas + new balance

            // Return balance

            keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.NEO.Transfer(snapshot, to, from, BigInteger.One, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(0);
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount);  // Remove neo balance from address two, Add umclaimed gas for from

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

            NativeContract.NEO.BalanceOf(snapshot, account).Should().Be(50_000_008);

            account[5]++; // Without existing balance

            NativeContract.NEO.BalanceOf(snapshot, account).Should().Be(0);
        }

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // StandbyValidators

            var validators = Check_GetValidators(snapshot);

            for (var x = 0; x < Blockchain.StandbyValidators.Length; x++)
            {
                validators[x].Equals(Blockchain.StandbyValidators[x]);
            }

            // Check double call

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.NEO.Script);

            var result = NativeContract.NEO.Initialize(engine);

            result.Should().Be(false);
        }

        [TestMethod]
        public void Check_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Blockchain.Singleton.GetSnapshot(), 0);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            NativeContract.NEO.Invoke(engine).Should().BeFalse();
        }

        [TestMethod]
        public void TestCalculateBonus()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            StorageKey key = CreateStorageKey(20, UInt160.Zero.ToArray());
            snapshot.Storages.Add(key, new StorageItem(new AccountState
            {
                Balance = -100
            }));
            Action action = () => NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            action.Should().Throw<ArgumentOutOfRangeException>();
            snapshot.Storages.Delete(key);

            UInt160 account = Contract.CreateSignatureContract(Blockchain.StandbyValidators[0]).ScriptHash;
            StorageItem item = snapshot.Storages.GetAndChange(CreateStorageKey(20, account.ToArray()), () => new StorageItem(new AccountState()));
            item.GetInteroperable<AccountState>().Balance = 100;
            item.GetInteroperable<AccountState>().BalanceHeight = 0;

            snapshot.PersistingBlock = new Block
            {
                Index = Blockchain.Epoch * 5 + 10
            };
            SetEpochState(snapshot);
            NativeContract.NEO.UnclaimedGas(snapshot, account, 1 * Blockchain.DecrementInterval).Should().Be(new BigInteger(45959217523));
        }

        private void SetEpochState(StoreView snapshot)
        {
            StorageKey epochKey = CreateStorageKey(17);
            snapshot.Storages.Add(epochKey, new StorageItem(new EpochState
            {
                CommitteeId = 1,
                EconomicId = 1
            }));

            snapshot.Storages.Add(CreateStorageKey(19, BitConverter.GetBytes((uint)0)), new StorageItem(new EconomicEpochState
            {
                GasPerBlock = 5 * NativeContract.GAS.Factor,
                NeoHoldersRewardRatio = 10,
                CommitteesRewardRatio = 5,
                VotersRewardRatio = 85,
                Start = 0,
                End = Blockchain.Epoch * 2 + 1,
            }));
            snapshot.Storages.Add(CreateStorageKey(19, BitConverter.GetBytes((uint)1)), new StorageItem(new EconomicEpochState
            {
                GasPerBlock = 2 * NativeContract.GAS.Factor,
                NeoHoldersRewardRatio = 10,
                CommitteesRewardRatio = 10,
                VotersRewardRatio = 80,
                Start = Blockchain.Epoch * 2 + 1,
                End = Blockchain.Epoch * 8 + 1,
            }));

            var i = 0;
            (ECPoint, BigInteger, UInt160)[] committees = new (ECPoint, BigInteger, UInt160)[Blockchain.CommitteeMembersCount];
            foreach (ECPoint committee in Blockchain.StandbyCommittee)
            {
                committees[i].Item1 = committee;
                committees[i].Item2 = 10000;
                committees[i].Item3 = Contract.CreateSignatureContract(committee).ScriptHash;
                i++;
            }
            snapshot.Storages.Add(CreateStorageKey(23, BitConverter.GetBytes((uint)0)), new StorageItem(new CommitteesEpochState
            {
                Start = 0,
                End = Blockchain.Epoch * 3 + 1,
                Committees = committees
            }));

            var j = Blockchain.CommitteeMembersCount - 1;
            (ECPoint, BigInteger, UInt160)[] committees2 = new (ECPoint, BigInteger, UInt160)[Blockchain.CommitteeMembersCount];
            foreach (ECPoint committee in Blockchain.StandbyCommittee)
            {
                committees2[j].Item1 = committee;
                committees2[j].Item2 = 20000;
                committees2[j].Item3 = Contract.CreateSignatureContract(committee).ScriptHash;
                j--;
            }
            snapshot.Storages.Add(CreateStorageKey(23, BitConverter.GetBytes((uint)1)), new StorageItem(new CommitteesEpochState
            {
                Start = Blockchain.Epoch * 3 + 1,
                End = Blockchain.Epoch * 7 + 1,
                Committees = committees2
            }));
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
        public void TestGetRegisteredValidators1()
        {
            using (ApplicationEngine engine = NativeContract.NEO.TestCall("getCandidates"))
            {
                engine.ResultStack.TryPop(out VM.Types.Array array).Should().BeTrue();
                array.Count.Should().Be(21);
                ((VM.Types.Struct)array[0])[0].GetSpan().ToHexString().Should().Be("020f2887f41474cfeb11fd262e982051c1541418137c02a0f4961af911045de639");
                ((VM.Types.Struct)array[0])[1].GetBigInteger().Should().Be(new BigInteger(1785714));
                ((VM.Types.Struct)array[1])[0].GetSpan().ToHexString().Should().Be("0222038884bbd1d8ff109ed3bdef3542e768eef76c1247aea8bc8171f532928c30");
                ((VM.Types.Struct)array[1])[1].GetBigInteger().Should().Be(new BigInteger(1785714));
                ((VM.Types.Struct)array[2])[0].GetSpan().ToHexString().Should().Be("0226933336f1b75baa42d42b71d9091508b638046d19abd67f4e119bf64a7cfb4d");
                ((VM.Types.Struct)array[2])[1].GetBigInteger().Should().Be(new BigInteger(1785714));
                ((VM.Types.Struct)array[3])[0].GetSpan().ToHexString().Should().Be("023a36c72844610b4d34d1968662424011bf783ca9d984efa19a20babf5582f3fe");
                ((VM.Types.Struct)array[3])[1].GetBigInteger().Should().Be(new BigInteger(1785714));
                ((VM.Types.Struct)array[4])[0].GetSpan().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
                ((VM.Types.Struct)array[4])[1].GetBigInteger().Should().Be(new BigInteger(3571428));
                ((VM.Types.Struct)array[5])[0].GetSpan().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
                ((VM.Types.Struct)array[5])[1].GetBigInteger().Should().Be(new BigInteger(3571428));
                ((VM.Types.Struct)array[6])[0].GetSpan().ToHexString().Should().Be("02504acbc1f4b3bdad1d86d6e1a08603771db135a73e61c9d565ae06a1938cd2ad");
                ((VM.Types.Struct)array[6])[1].GetBigInteger().Should().Be(new BigInteger(1785714));
            }
        }

        [TestMethod]
        public void TestGetRegisteredValidators2()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var result = NativeContract.NEO.GetCandidates(snapshot).ToArray();
            result.Length.Should().Be(21);
            result[0].PublicKey.ToArray().ToHexString().Should().Be("020f2887f41474cfeb11fd262e982051c1541418137c02a0f4961af911045de639");
            result[0].Votes.Should().Be(new BigInteger(1785714));
            result[1].PublicKey.ToArray().ToHexString().Should().Be("0222038884bbd1d8ff109ed3bdef3542e768eef76c1247aea8bc8171f532928c30");
            result[1].Votes.Should().Be(new BigInteger(1785714));
            result[2].PublicKey.ToArray().ToHexString().Should().Be("0226933336f1b75baa42d42b71d9091508b638046d19abd67f4e119bf64a7cfb4d");
            result[2].Votes.Should().Be(new BigInteger(1785714));
            result[3].PublicKey.ToArray().ToHexString().Should().Be("023a36c72844610b4d34d1968662424011bf783ca9d984efa19a20babf5582f3fe");
            result[3].Votes.Should().Be(new BigInteger(1785714));
            result[4].PublicKey.ToArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
            result[4].Votes.Should().Be(new BigInteger(3571428));
            result[5].PublicKey.ToArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
            result[5].Votes.Should().Be(new BigInteger(3571428));
            result[6].PublicKey.ToArray().ToHexString().Should().Be("02504acbc1f4b3bdad1d86d6e1a08603771db135a73e61c9d565ae06a1938cd2ad");
            result[6].Votes.Should().Be(new BigInteger(1785714));

            StorageKey key = NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G);
            snapshot.Storages.Add(key, new StorageItem(new CandidateState()));
            NativeContract.NEO.GetCandidates(snapshot).ToArray().Length.Should().Be(22);
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
        public void TestInitialize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = new ApplicationEngine(TriggerType.System, null, snapshot, 0, true);
            Action action = () => NativeContract.NEO.Initialize(engine);
            action.Should().Throw<InvalidOperationException>();

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            NativeContract.NEO.Initialize(engine).Should().BeFalse();

            snapshot.Storages.Delete(CreateStorageKey(11));
            snapshot.PersistingBlock = Blockchain.GenesisBlock;
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            NativeContract.NEO.Initialize(engine).Should().BeTrue();
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
        public void TestUnclaimedGas()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            snapshot.Storages.Add(CreateStorageKey(20, UInt160.Zero.ToArray()), new StorageItem(new AccountState()));
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

            snapshot.Storages.Add(keyAccount, new StorageItem(new AccountState()));
            ret = Check_Vote(snapshot, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.Delete(keyAccount);
            snapshot.Storages.GetAndChange(keyAccount, () => new StorageItem(new AccountState
            {
                VoteTo = ECCurve.Secp256r1.G
            }));
            snapshot.Storages.Add(keyValidator, new StorageItem(new CandidateState()));
            ret = Check_Vote(snapshot, account.ToArray(), ECCurve.Secp256r1.G.ToArray(), true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
        }

        [TestMethod]
        public void TestEconomicParameter()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            (bool, bool) ret1 = Check_SetEconomicParameter(snapshot, "setGasPerBlock", 2 * GasToken.GAS.Factor);
            ret1.Item2.Should().BeTrue();
            ret1.Item1.Should().BeTrue();

            ret1 = Check_SetEconomicParameter(snapshot, "setNeoHoldersRewardRatio", 10);
            ret1.Item2.Should().BeTrue();
            ret1.Item1.Should().BeTrue();

            ret1 = Check_SetEconomicParameter(snapshot, "setCommitteesRewardRatio", 10);
            ret1.Item2.Should().BeTrue();
            ret1.Item1.Should().BeTrue();

            ret1 = Check_SetEconomicParameter(snapshot, "setVotersRewardRatio", 80);
            ret1.Item2.Should().BeTrue();
            ret1.Item1.Should().BeTrue();

            (BigInteger, bool) result = Check_GetEconomicParameter(snapshot, "getGasPerBlock");
            result.Item2.Should().BeTrue();
            result.Item1.Should().Be(2 * GasToken.GAS.Factor);

            result = Check_GetEconomicParameter(snapshot, "getGasPerBlock");
            result.Item2.Should().BeTrue();
            result.Item1.Should().Be(2 * GasToken.GAS.Factor);

            result = Check_GetEconomicParameter(snapshot, "getNeoHoldersRewardRatio");
            result.Item2.Should().BeTrue();
            result.Item1.Should().Be(10);

            result = Check_GetEconomicParameter(snapshot, "getCommitteesRewardRatio");
            result.Item2.Should().BeTrue();
            result.Item1.Should().Be(10);

            result = Check_GetEconomicParameter(snapshot, "getVotersRewardRatio");
            result.Item2.Should().BeTrue();
            result.Item1.Should().Be(80);
        }

        [TestMethod]
        public void TestCrossBlockEpoch()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // 1) epoch parameter will keep the old one
            snapshot.PersistingBlock = new Block() { Index = Blockchain.Epoch };

            InvokeNeoTokenOnPersist(snapshot);

            EpochState epochState = snapshot.Storages.TryGet(CreateStorageKey(17)).GetInteroperable<EpochState>();
            epochState.CommitteeId.Should().Be(0);
            epochState.EconomicId.Should().Be(0);

            // 2) epoch pamaeter will adjusted, when votes or economic parameters changed
            snapshot.PersistingBlock = new Block() { Index = Blockchain.Epoch * 2 };
            for (int i = 0; i < 7; i++)
            {
                StorageKey key = CreateStorageKey(33, Blockchain.StandbyCommittee[Blockchain.CommitteeMembersCount - i - 1].ToArray());
                CandidateState candidate = snapshot.Storages.TryGet(key).GetInteroperable<CandidateState>();
                candidate.Votes *= 10;
            }
            EconomicParameter economic = snapshot.Storages.GetAndChange(CreateStorageKey(27)).GetInteroperable<EconomicParameter>();
            economic.NeoHoldersRewardRatio = 10;
            economic.CommitteesRewardRatio = 10;
            economic.VotersRewardRatio = 80;

            InvokeNeoTokenOnPersist(snapshot);

            epochState = snapshot.Storages.TryGet(CreateStorageKey(17)).GetInteroperable<EpochState>();
            epochState.CommitteeId.Should().Be(1);
            epochState.EconomicId.Should().Be(1);

            EconomicEpochState economicEpoch = snapshot.Storages.TryGet(CreateStorageKey(19, BitConverter.GetBytes((uint)1))).GetInteroperable<EconomicEpochState>();
            economicEpoch.NeoHoldersRewardRatio.Should().Be(10);
            economicEpoch.CommitteesRewardRatio.Should().Be(10);
            economicEpoch.VotersRewardRatio.Should().Be(80);
            CommitteesEpochState committeesEpoch = snapshot.Storages.TryGet(CreateStorageKey(23, BitConverter.GetBytes((uint)1))).GetInteroperable<CommitteesEpochState>();
            for(var i = 0; i < 7; i++)
            {
                var validator = committeesEpoch.Committees[i].Item1;
                bool found = false;
                for(var j = 0; j < 7; j++)
                {
                    if (validator.Equals(Blockchain.StandbyCommittee[Blockchain.CommitteeMembersCount - j - 1]))
                    {
                        found = true;
                        break;
                    }
                }
                found.Should().BeTrue();
            }
        }

        internal bool InvokeNeoTokenOnPersist(StoreView snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.System, null, snapshot, 0, true);
            engine.LoadScript(NativeContract.NEO.Script);
            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("onPersist");
            engine.LoadScript(script.ToArray());
            return engine.Execute() == VMState.HALT;
        }

        internal (bool State, bool Result) Transfer4TesingOnBalanceChanging(BigInteger amount, bool addVotes)
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.PersistingBlock = Blockchain.GenesisBlock;
            var engine = new ApplicationEngine(TriggerType.Application, Blockchain.GenesisBlock, snapshot, 0, true);
            ScriptBuilder sb = new ScriptBuilder();
            var tmp = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot);
            UInt160 from = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot)[0];
            if (addVotes)
            {
                snapshot.Storages.Add(CreateStorageKey(20, from.ToArray()), new StorageItem(new AccountState
                {
                    VoteTo = ECCurve.Secp256r1.G,
                    Balance = new BigInteger(1000)
                }));
                snapshot.Storages.Add(NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G), new StorageItem(new CandidateState()));
            }
            else
            {
                snapshot.Storages.Add(CreateStorageKey(20, from.ToArray()), new StorageItem(new AccountState
                {
                    Balance = new BigInteger(1000)
                }));
            }

            sb.EmitAppCall(NativeContract.NEO.Hash, "transfer", from, UInt160.Zero, amount);
            engine.LoadScript(sb.ToArray());
            engine.Execute();
            var result = engine.ResultStack.Peek();
            result.GetType().Should().Be(typeof(VM.Types.Boolean));
            return (true, result.ToBoolean());
        }

        internal static (bool State, bool Result) Check_Vote(StoreView snapshot, byte[] account, byte[] pubkey, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

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

            return (true, result.ToBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterValidator(StoreView snapshot, byte[] pubkey)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1)).ToScriptHash()), snapshot, 0, true);

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

            return (true, result.ToBoolean());
        }

        internal static ECPoint[] Check_GetValidators(StoreView snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

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
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

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

            return ((result as VM.Types.Integer).GetBigInteger(), true);
        }

        internal static (BigInteger Value, bool State) Check_GetEconomicParameter(StoreView snapshot, string method)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush(method);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return ((result as VM.Types.Integer).GetBigInteger(), true);
        }

        internal static (bool Value, bool State) Check_SetEconomicParameter(StoreView snapshot, string method, BigInteger value)
        {
            ECPoint[] committees = NeoToken.NEO.GetCommittee(snapshot);
            UInt160 committeesMultisign = Contract.CreateMultiSigRedeemScript(committees.Length - (committees.Length - 1) / 3, committees).ToScriptHash();
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(committeesMultisign), snapshot, 0, true);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(value);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush(method);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (result.ToBoolean(), true);
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

            st[0].GetBigInteger().Should().Be(balance); // Balance
            st[1].GetBigInteger().Should().Be(height);  // BalanceHeight
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
    }
}
