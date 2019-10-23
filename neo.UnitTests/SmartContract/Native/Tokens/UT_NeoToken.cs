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
        private Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
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
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            // No signature

            var ret = Check_Vote(snapshot, from, new byte[][] { }, false);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();

            // Wrong address

            ret = Check_Vote(snapshot, new byte[19], new byte[][] { }, false);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeFalse();

            // Wrong ec

            ret = Check_Vote(snapshot, from, new byte[][] { new byte[19] }, true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeFalse();

            // no registered

            var fakeAddr = new byte[20];
            fakeAddr[0] = 0x5F;
            fakeAddr[5] = 0xFF;

            ret = Check_Vote(snapshot, fakeAddr, new byte[][] { }, true);
            ret.Result.Should().BeFalse();
            ret.State.Should().BeTrue();

            // TODO: More votes tests
        }

        [TestMethod]
        public void Check_UnclaimedGas()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(600000000000));
            unclaim.State.Should().BeTrue();

            unclaim = Check_UnclaimedGas(snapshot, new byte[19]);
            unclaim.Value.Should().Be(BigInteger.Zero);
            unclaim.State.Should().BeFalse();
        }

        [TestMethod]
        public void Check_RegisterValidator()
        {
            var snapshot = Store.GetSnapshot().Clone();

            var ret = Check_RegisterValidator(snapshot, new byte[0]);
            ret.State.Should().BeFalse();
            ret.Result.Should().BeFalse();

            ret = Check_RegisterValidator(snapshot, new byte[33]);
            ret.State.Should().BeFalse();
            ret.Result.Should().BeFalse();

            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var point = Blockchain.StandbyValidators[0].EncodePoint(true);

            ret = Check_RegisterValidator(snapshot, point); // Exists
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount); // No changes

            point[20]++; // fake point
            ret = Check_RegisterValidator(snapshot, point); // New

            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1); // New validator

            // Check GetRegisteredValidators

            var validators = NativeContract.NEO.GetRegisteredValidators(snapshot).OrderBy(u => u.PublicKey).ToArray();
            var check = Blockchain.StandbyValidators.Select(u => u.EncodePoint(true)).ToList();
            check.Add(point); // Add the new member

            for (int x = 0; x < validators.Length; x++)
            {
                Assert.AreEqual(1, check.RemoveAll(u => u.SequenceEqual(validators[x].PublicKey.EncodePoint(true))));
                Assert.AreEqual(0, validators[x].Votes);
            }

            Assert.AreEqual(0, check.Count);
        }

        [TestMethod]
        public void Check_Transfer()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            byte[] to = new byte[20];

            var keyCount = snapshot.Storages.GetChangeSet().Count();

            // Check unclaim

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(600000000000));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.One, false).Should().BeFalse(); // Not signed
            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.One, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(99_999_999);
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
            var snapshot = Store.GetSnapshot().Clone();
            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            NativeContract.NEO.BalanceOf(snapshot, account).Should().Be(100_000_000);

            account[5]++; // Without existing balance

            NativeContract.NEO.BalanceOf(snapshot, account).Should().Be(0);
        }

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Store.GetSnapshot().Clone();

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
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), 0);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            NativeContract.NEO.Invoke(engine).Should().BeFalse();
        }

        [TestMethod]
        public void TestCalculateBonus()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            StorageKey key = CreateStorageKey(20, UInt160.Zero.ToArray());
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = new AccountState()
                {
                    Balance = -100
                }.ToByteArray()
            });
            Action action = () => NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            action.Should().Throw<ArgumentOutOfRangeException>();
            snapshot.Storages.Delete(key);
            snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new AccountState()
                {
                    Balance = 100
                }.ToByteArray()
            });
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 30 * Blockchain.DecrementInterval).Should().Be(new BigInteger(7000000000));
        }

        [TestMethod]
        public void TestGetNextBlockValidators1()
        {
            using (ApplicationEngine engine = NativeContract.NEO.TestCall("getNextBlockValidators"))
            {
                var result = engine.ResultStack.Peek();
                result.GetType().Should().Be(typeof(VM.Types.Array));
                ((VM.Types.Array)result).Count.Should().Be(7);
                ((VM.Types.ByteArray)((VM.Types.Array)result)[0]).GetByteArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[1]).GetByteArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[2]).GetByteArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[3]).GetByteArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[4]).GetByteArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[5]).GetByteArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[6]).GetByteArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
            }
        }

        [TestMethod]
        public void TestGetNextBlockValidators2()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
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
            using (ApplicationEngine engine = NativeContract.NEO.TestCall("getRegisteredValidators"))
            {
                var result = engine.ResultStack.Peek();
                result.GetType().Should().Be(typeof(VM.Types.Array));
                ((VM.Types.Array)result).Count.Should().Be(7);
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[0])[0]).GetByteArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
                ((VM.Types.Struct)((VM.Types.Array)result)[0])[1].GetBigInteger().Should().Be(new BigInteger(0));
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[1])[0]).GetByteArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
                ((VM.Types.Struct)((VM.Types.Array)result)[1])[1].GetBigInteger().Should().Be(new BigInteger(0));
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[2])[0]).GetByteArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
                ((VM.Types.Struct)((VM.Types.Array)result)[2])[1].GetBigInteger().Should().Be(new BigInteger(0));
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[3])[0]).GetByteArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
                ((VM.Types.Struct)((VM.Types.Array)result)[3])[1].GetBigInteger().Should().Be(new BigInteger(0));
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[4])[0]).GetByteArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
                ((VM.Types.Struct)((VM.Types.Array)result)[4])[1].GetBigInteger().Should().Be(new BigInteger(0));
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[5])[0]).GetByteArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
                ((VM.Types.Struct)((VM.Types.Array)result)[5])[1].GetBigInteger().Should().Be(new BigInteger(0));
                ((VM.Types.ByteArray)((VM.Types.Struct)((VM.Types.Array)result)[6])[0]).GetByteArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
                ((VM.Types.Struct)((VM.Types.Array)result)[6])[1].GetBigInteger().Should().Be(new BigInteger(0));
            }
        }

        [TestMethod]
        public void TestGetRegisteredValidators2()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            var result = NativeContract.NEO.GetRegisteredValidators(snapshot).ToArray();
            result.Length.Should().Be(7);
            result[0].PublicKey.ToArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
            result[0].Votes.Should().Be(new BigInteger(0));
            result[1].PublicKey.ToArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
            result[1].Votes.Should().Be(new BigInteger(0));
            result[2].PublicKey.ToArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
            result[2].Votes.Should().Be(new BigInteger(0));
            result[3].PublicKey.ToArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
            result[3].Votes.Should().Be(new BigInteger(0));
            result[4].PublicKey.ToArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
            result[4].Votes.Should().Be(new BigInteger(0));
            result[5].PublicKey.ToArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
            result[5].Votes.Should().Be(new BigInteger(0));
            result[6].PublicKey.ToArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
            result[6].Votes.Should().Be(new BigInteger(0));

            StorageKey key = NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G);
            snapshot.Storages.Add(key, new StorageItem
            {
                Value = new ValidatorState().ToByteArray()
            });
            NativeContract.NEO.GetRegisteredValidators(snapshot).ToArray().Length.Should().Be(8);
        }

        [TestMethod]
        public void TestGetValidators1()
        {
            using (ApplicationEngine engine = NativeContract.NEO.TestCall("getValidators"))
            {
                var result = engine.ResultStack.Peek();
                result.GetType().Should().Be(typeof(VM.Types.Array));
                ((VM.Types.Array)result).Count.Should().Be(7);
                ((VM.Types.ByteArray)((VM.Types.Array)result)[0]).GetByteArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[1]).GetByteArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[2]).GetByteArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[3]).GetByteArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[4]).GetByteArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[5]).GetByteArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
                ((VM.Types.ByteArray)((VM.Types.Array)result)[6]).GetByteArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");
            }
        }

        [TestMethod]
        public void TestGetValidators2()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            var result = NativeContract.NEO.GetValidators(snapshot);
            result[0].ToArray().ToHexString().Should().Be("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c");
            result[1].ToArray().ToHexString().Should().Be("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093");
            result[2].ToArray().ToHexString().Should().Be("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a");
            result[3].ToArray().ToHexString().Should().Be("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554");
            result[4].ToArray().ToHexString().Should().Be("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d");
            result[5].ToArray().ToHexString().Should().Be("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e");
            result[6].ToArray().ToHexString().Should().Be("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70");

            StorageKey key = CreateStorageKey(15);
            ValidatorsCountState state = new ValidatorsCountState();
            for (int i = 0; i < 100; i++)
            {
                state.Votes[i] = new BigInteger(i + 1);
            }
            snapshot.Storages.Add(key, new StorageItem()
            {
                Value = state.ToByteArray()
            });
            NativeContract.NEO.GetValidators(snapshot).ToArray().Length.Should().Be(7);
        }

        [TestMethod]
        public void TestInitialize()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            var engine = new ApplicationEngine(TriggerType.System, null, snapshot, 0, true);
            Action action = () => NativeContract.NEO.Initialize(engine);
            action.Should().Throw<InvalidOperationException>();

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            NativeContract.NEO.Initialize(engine).Should().BeFalse();

            snapshot.Storages.Delete(CreateStorageKey(11));
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
            Snapshot snapshot = Store.GetSnapshot().Clone();
            NativeContract.NEO.TotalSupply(snapshot).Should().Be(new BigInteger(100000000));
        }

        [TestMethod]
        public void TestUnclaimedGas()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
            snapshot.Storages.Add(CreateStorageKey(20, UInt160.Zero.ToArray()), new StorageItem
            {
                Value = new AccountState().ToByteArray()
            });
            NativeContract.NEO.UnclaimedGas(snapshot, UInt160.Zero, 10).Should().Be(new BigInteger(0));
        }

        [TestMethod]
        public void TestVote()
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            UInt160 account = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
            StorageKey keyAccount = CreateStorageKey(20, account.ToArray());
            StorageKey keyValidator = CreateStorageKey(33, ECCurve.Secp256r1.G.ToArray());
            var ret = Check_Vote(snapshot, account.ToArray(), new byte[][] { ECCurve.Secp256r1.G.ToArray() }, false);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            ret = Check_Vote(snapshot, account.ToArray(), new byte[][] { ECCurve.Secp256r1.G.ToArray() }, true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.Add(keyAccount, new StorageItem
            {
                Value = new AccountState().ToByteArray()
            });
            ret = Check_Vote(snapshot, account.ToArray(), new byte[][] { ECCurve.Secp256r1.G.ToArray() }, true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            snapshot.Storages.Delete(keyAccount);
            snapshot.Storages.GetAndChange(keyAccount, () => new StorageItem
            {
                Value = new AccountState()
                {
                    Votes = new ECPoint[] { ECCurve.Secp256r1.G }
                }.ToByteArray()
            });
            snapshot.Storages.Add(keyValidator, new StorageItem
            {
                Value = new ValidatorState().ToByteArray()
            });
            ret = Check_Vote(snapshot, account.ToArray(), new byte[][] { ECCurve.Secp256r1.G.ToArray() }, true);
            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();
        }

        [TestMethod]
        public void TestValidatorsCountState_FromByteArray()
        {
            ValidatorsCountState input = new ValidatorsCountState { Votes = new BigInteger[] { new BigInteger(1000) } };
            ValidatorsCountState output = ValidatorsCountState.FromByteArray(input.ToByteArray());
            output.Should().BeEquivalentTo(input);
        }

        [TestMethod]
        public void TestValidatorState_FromByteArray()
        {
            ValidatorState input = new ValidatorState { Votes = new BigInteger(1000) };
            ValidatorState output = ValidatorState.FromByteArray(input.ToByteArray());
            output.Should().BeEquivalentTo(input);
        }

        [TestMethod]
        public void TestValidatorState_ToByteArray()
        {
            ValidatorState input = new ValidatorState { Votes = new BigInteger(1000) };
            input.ToByteArray().ToHexString().Should().Be("e803");
        }

        internal (bool State, bool Result) Transfer4TesingOnBalanceChanging(BigInteger amount, bool addVotes)
        {
            Snapshot snapshot = Store.GetSnapshot().Clone();
            var engine = new ApplicationEngine(TriggerType.Application, Blockchain.GenesisBlock, snapshot, 0, true);
            ScriptBuilder sb = new ScriptBuilder();
            var tmp = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot);
            UInt160 from = engine.ScriptContainer.GetScriptHashesForVerifying(engine.Snapshot)[0];
            if (addVotes)
            {
                snapshot.Storages.Add(CreateStorageKey(20, from.ToArray()), new StorageItem
                {
                    Value = new AccountState()
                    {
                        Votes = new ECPoint[] { ECCurve.Secp256r1.G },
                        Balance = new BigInteger(1000)
                    }.ToByteArray()
                });
                snapshot.Storages.Add(NativeContract.NEO.CreateStorageKey(33, ECCurve.Secp256r1.G), new StorageItem
                {
                    Value = new ValidatorState().ToByteArray()
                });

                ValidatorsCountState state = new ValidatorsCountState();
                for (int i = 0; i < 100; i++)
                {
                    state.Votes[i] = new BigInteger(i + 1);
                }
                snapshot.Storages.Add(CreateStorageKey(15), new StorageItem()
                {
                    Value = state.ToByteArray()
                });
            }
            else
            {
                snapshot.Storages.Add(CreateStorageKey(20, from.ToArray()), new StorageItem
                {
                    Value = new AccountState()
                    {
                        Balance = new BigInteger(1000)
                    }.ToByteArray()
                });
            }

            sb.EmitAppCall(NativeContract.NEO.Hash, "transfer", from, UInt160.Zero, amount);
            engine.LoadScript(sb.ToArray());
            engine.Execute();
            var result = engine.ResultStack.Peek();
            result.GetType().Should().Be(typeof(VM.Types.Boolean));
            return (true, (result as VM.Types.Boolean).GetBoolean());
        }

        internal static (bool State, bool Result) Check_Vote(Snapshot snapshot, byte[] account, byte[][] pubkeys, bool signAccount)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new Nep5NativeContractExtensions.ManualWitness(signAccount ? new UInt160(account) : UInt160.Zero), snapshot, 0, true);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();

            foreach (var ec in pubkeys) script.EmitPush(ec);
            script.EmitPush(pubkeys.Length);
            script.Emit(OpCode.PACK);

            script.EmitPush(account.ToArray());
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

            return (true, (result as VM.Types.Boolean).GetBoolean());
        }

        internal static (bool State, bool Result) Check_RegisterValidator(Snapshot snapshot, byte[] pubkey)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);

            engine.LoadScript(NativeContract.NEO.Script);

            var script = new ScriptBuilder();
            script.EmitPush(pubkey);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("registerValidator");
            engine.LoadScript(script.ToArray());

            if (engine.Execute() == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, (result as VM.Types.Boolean).GetBoolean());
        }

        internal static ECPoint[] Check_GetValidators(Snapshot snapshot)
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

            return (result as VM.Types.Array).Select(u => u.GetByteArray().AsSerializable<ECPoint>()).ToArray();
        }

        internal static (BigInteger Value, bool State) Check_UnclaimedGas(Snapshot snapshot, byte[] address)
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

        internal static void CheckValidator(ECPoint eCPoint, DataCache<StorageKey, StorageItem>.Trackable trackable)
        {
            var st = new BigInteger(trackable.Item.Value);
            st.Should().Be(0);

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)));
            trackable.Item.IsConstant.Should().Be(false);
        }

        internal static void CheckBalance(byte[] account, DataCache<StorageKey, StorageItem>.Trackable trackable, BigInteger balance, BigInteger height, ECPoint[] votes)
        {
            var st = (VM.Types.Struct)trackable.Item.Value.DeserializeStackItem(3, 32);

            st.Count.Should().Be(3);
            st.Select(u => u.GetType()).ToArray().Should().BeEquivalentTo(new Type[] { typeof(VM.Types.Integer), typeof(VM.Types.Integer), typeof(VM.Types.ByteArray) }); // Balance

            st[0].GetBigInteger().Should().Be(balance); // Balance
            st[1].GetBigInteger().Should().Be(height);  // BalanceHeight
            (st[2].GetByteArray().AsSerializableArray<ECPoint>(Blockchain.MaxValidators)).Should().BeEquivalentTo(votes);  // Votes

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 20 }.Concat(account));
            trackable.Item.IsConstant.Should().Be(false);
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = NativeContract.NEO.Hash,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }
    }
}
