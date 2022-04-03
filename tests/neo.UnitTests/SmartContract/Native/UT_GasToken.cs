using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_GasToken
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
        public void Check_Name() => NativeContract.GAS.Name.Should().Be(nameof(GasToken));

        [TestMethod]
        public void Check_Symbol() => NativeContract.GAS.Symbol(_snapshot).Should().Be("GAS");

        [TestMethod]
        public void Check_Decimals() => NativeContract.GAS.Decimals(_snapshot).Should().Be(8);

        // [TestMethod]
        // public async Task Check_BalanceOfTransferAndBurn()
        // {
        //     var snapshot = _snapshot.CreateSnapshot();
        //     var persistingBlock = new Block { Header = new Header { Index = 1000 } };
        //     byte[] from = Contract.GetBFTAddress(ProtocolSettings.Default.StandbyValidators).ToArray();
        //     byte[] to = new byte[20];
        //     var keyCount = snapshot.GetChangeSet().Count();
        //     var supply = NativeContract.GAS.TotalSupply(snapshot);
        //     supply.Should().Be(5200000050000000); // 3000000000000000 + 50000000 (neo holder reward)

        //     // Check unclaim

        //     var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
        //     unclaim.Value.Should().Be(new BigInteger(0.5 * 1000 * 100000000L));
        //     unclaim.State.Should().BeTrue();

        //     // Transfer

        //     NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true, persistingBlock).Should().BeTrue();
        //     Assert.ThrowsException<ArgumentNullException>(() => NativeContract.NEO.Transfer(snapshot, from, null, BigInteger.Zero, true, persistingBlock));
        //     Assert.ThrowsException<ArgumentNullException>(() => NativeContract.NEO.Transfer(snapshot, null, to, BigInteger.Zero, false, persistingBlock));
        //     NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(100000000);
        //     NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(0);

        //     NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(52000500_00000000);
        //     NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(0);

        //     // Check unclaim

        //     unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
        //     unclaim.Value.Should().Be(new BigInteger(0));
        //     unclaim.State.Should().BeTrue();

        //     supply = NativeContract.GAS.TotalSupply(snapshot);
        //     supply.Should().Be(5200050050000000);

        //     snapshot.GetChangeSet().Count().Should().Be(keyCount + 3); // Gas

        //     // Transfer

        //     keyCount = snapshot.GetChangeSet().Count();

        //     NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, false, persistingBlock).Should().BeFalse(); // Not signed
        //     NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000001, true, persistingBlock).Should().BeFalse(); // More than balance
        //     NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, true, persistingBlock).Should().BeTrue(); // All balance

        //     // Balance of

        //     NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(52000500_00000000);
        //     NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(0);

        //     snapshot.GetChangeSet().Count().Should().Be(keyCount + 1); // All

        //     // Burn

        //     using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 0);
        //     keyCount = snapshot.GetChangeSet().Count();

        //     await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
        //         await NativeContract.GAS.Burn(engine, new UInt160(to), BigInteger.MinusOne));

        //     // Burn more than expected

        //     await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        //         await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(52000500_00000001)));

        //     // Real burn

        //     await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(1));

        //     NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(5200049999999999);

        //     keyCount.Should().Be(snapshot.GetChangeSet().Count());

        //     // Burn all

        //     await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(5200049999999999));

        //     (keyCount - 1).Should().Be(snapshot.GetChangeSet().Count());

        //     // Bad inputs

        //     Assert.ThrowsException<ArgumentOutOfRangeException>(() => NativeContract.GAS.Transfer(snapshot, from, to, BigInteger.MinusOne, true, persistingBlock));
        //     Assert.ThrowsException<ArgumentOutOfRangeException>(() => NativeContract.GAS.Transfer(snapshot, new byte[19], to, BigInteger.One, false, persistingBlock));
        //     Assert.ThrowsException<ArgumentOutOfRangeException>(() => NativeContract.GAS.Transfer(snapshot, from, new byte[19], BigInteger.One, false, persistingBlock));
        // }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new()
            {
                Id = NativeContract.GAS.Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }
    }
}
