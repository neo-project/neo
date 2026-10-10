// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountState
    {
        private static UInt160 Address(byte n) => new(Enumerable.Repeat(n, 20).ToArray());
        private static UInt256 Hash(byte n) => new(Enumerable.Repeat(n, 32).ToArray());
        private static SmartAccountModuleBinding Binding(byte n) => new(Address(n), Hash(n));
        private static SmartAccountState Fresh(bool recovery = true) => SmartAccountState.Create(123, Address(1), new byte[32], Binding(3), Binding(4), recovery ? Address(2) : UInt160.Zero, true);
        private static byte[] Encode(StackItem item) => BinarySerializer.Serialize(item, 4096, 256);
        private static SmartAccountState Read(Array value) => SmartAccountState.Deserialize(Fresh().AccountId, Encode(value));
        private static SmartAccountState AtEpoch(ulong epoch)
        {
            var item = Fresh().ToStackItem();
            item[8] = new BigInteger(epoch);
            return Read(item);
        }
        private static void Invalid(Action action) => Assert.ThrowsExactly<InvalidOperationException>(action);
        private static void Malformed(Array record) => Assert.ThrowsExactly<FormatException>(() => Read(record));
        private static SmartAccountState Pending() => Fresh()
            .ProposeModule(SmartAccountModuleKind.Verifier, Binding(5), 10, true)
            .ProposeModule(SmartAccountModuleKind.Hook, null, 10, true)
            .ProposeRecoveryAddress(Address(6), 10, true)
            .ProposeRecovery(Address(7), 10, true);
        private static void EmptyIntents(SmartAccountState state)
        {
            var item = state.ToStackItem();
            for (int i = 9; i < 13; i++) Assert.IsInstanceOfType<Null>(item[i]);
        }

        [TestMethod]
        public void RegistrationAndFullRecordRoundTrip()
        {
            var state = Fresh();
            Assert.AreEqual(SmartAccountStatus.Active, state.Status);
            Assert.AreEqual(0UL, state.ConfigurationNonce);
            Assert.AreEqual(SmartAccountProtocol.GetAccountId(123, Address(1), new byte[32]), state.AccountId);
            Assert.AreEqual(SmartAccountProtocol.GetAccountAddress(state.AccountId), state.AccountAddress);
            EmptyIntents(state);
            foreach (var current in new[] { state, Fresh(false), Pending(), state.Freeze(true) })
            {
                Assert.AreEqual(14, current.ToStackItem().Count);
                Assert.AreSequenceEqual(current.Serialize(), SmartAccountState.Deserialize(current.AccountId, current.Serialize()).Serialize());
            }
        }

        [TestMethod]
        public void IndependentStateSerializationFixturesMatch()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "SmartContract", "Native", "TestFile", "smartaccount-state-v2.json");
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            SmartAccountState[] states = [Fresh(), Pending(), AtEpoch(ulong.MaxValue), Fresh(false).ProposeModule(SmartAccountModuleKind.Verifier, null, 0, true).ActivateModule(SmartAccountModuleKind.Verifier, SmartAccountState.ModuleChangeDelayMs)];
            int i = 0;
            foreach (var vector in document.RootElement.GetProperty("vectors").EnumerateArray())
            {
                byte[] expected = Convert.FromHexString(vector.GetProperty("serializedState").GetString());
                Assert.AreSequenceEqual(expected, states[i].Serialize(), vector.GetProperty("name").GetString());
                Assert.AreSequenceEqual(expected, SmartAccountState.Deserialize(states[i].AccountId, expected).Serialize());
                i++;
            }
            Assert.AreEqual(states.Length, i);
        }

        [TestMethod]
        public void CreationAndBindingsRejectInvalidIdentityOrAuthority()
        {
            Invalid(() => SmartAccountState.Create(123, Address(1), new byte[32], null, null, Address(2), false));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Create(123, UInt160.Zero, new byte[32], null, null, Address(2), true));
            Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Create(123, Address(1), new byte[31], null, null, Address(2), true));
            foreach (var recovery in new[] { Address(1), Fresh().AccountAddress })
                Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Create(123, Address(1), new byte[32], null, null, recovery, true));
            foreach (var contract in new[] { UInt160.Zero, NativeContract.GAS.Hash, SmartAccountProtocol.ServiceHash })
                Assert.ThrowsExactly<FormatException>(() => new SmartAccountModuleBinding(contract, Hash(1)));
            Assert.ThrowsExactly<ArgumentNullException>(() => new SmartAccountModuleBinding(null, Hash(1)));
            Assert.ThrowsExactly<ArgumentNullException>(() => new SmartAccountModuleBinding(Address(1), null));
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountState.Deserialize(null, Fresh().Serialize()));
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountState.Create(123, Address(1), new byte[32], null, null, null, true));
            // A hash value of zero is not a missing binding when the contract is nonzero.
            _ = new SmartAccountModuleBinding(Address(3), UInt256.Zero);
        }

        [TestMethod]
        public void NativeContractsCannotOwnCustodyOrRecoveryAuthority()
        {
            foreach (var native in NativeContract.Contracts)
            {
                Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Create(123, native.Hash, new byte[32], null, null, UInt160.Zero, true));
                Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Create(123, Address(1), new byte[32], null, null, native.Hash, true));
                Assert.ThrowsExactly<FormatException>(() => Fresh().ProposeRecoveryAddress(native.Hash, 10, true));
                Assert.ThrowsExactly<FormatException>(() => Fresh().ProposeRecovery(native.Hash, 10, true));
                var current = Fresh().ToStackItem(); current[3] = native.Hash.ToArray(); Malformed(current);
                current = Fresh().ToStackItem(); current[4] = native.Hash.ToArray(); Malformed(current);
                current = Pending().ToStackItem(); ((Array)current[11])[0] = native.Hash.ToArray(); Malformed(current);
                current = Pending().ToStackItem(); ((Array)current[12])[0] = native.Hash.ToArray(); Malformed(current);
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void DelayedModuleActivationAndRemoval(int value)
        {
            var kind = (SmartAccountModuleKind)value;
            var original = Fresh();
            byte[] before = original.Serialize();
            var pending = original.ProposeModule(kind, Binding(5), 10, true);
            Invalid(() => original.ProposeModule(kind, Binding(5), 10, false));
            Invalid(() => pending.ActivateModule(kind, 10 + SmartAccountState.ModuleChangeDelayMs - 1));
            var active = pending.ActivateModule(kind, 10 + SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(Address(5), (kind == SmartAccountModuleKind.Verifier ? active.Verifier : active.Hook).Contract);
            Assert.AreEqual(1UL, active.ConfigurationNonce);
            EmptyIntents(active);
            Assert.AreSequenceEqual(before, original.Serialize());
            var removed = active.ProposeModule(kind, null, 0, true).ActivateModule(kind, ulong.MaxValue);
            Assert.IsNull(kind == SmartAccountModuleKind.Verifier ? removed.Verifier : removed.Hook);
            var cancelled = pending.CancelModule(kind, true);
            Assert.AreSequenceEqual(before, cancelled.Serialize());
            Invalid(() => pending.CancelModule(kind, false));
            Invalid(() => original.CancelModule(kind, true));
            Invalid(() => original.ActivateModule(kind, ulong.MaxValue));
            var replacement = pending.ProposeModule(kind, null, 100, true);
            Invalid(() => replacement.ActivateModule(kind, 10 + SmartAccountState.ModuleChangeDelayMs));
            _ = replacement.ActivateModule(kind, 100 + SmartAccountState.ModuleChangeDelayMs);
        }

        [TestMethod]
        public void RecoveryAddressDelayRemovalCancellationAndAuthority()
        {
            var state = Fresh();
            var pending = state.ProposeRecoveryAddress(Address(6), 10, true);
            Invalid(() => state.ProposeRecoveryAddress(Address(6), 10, false));
            Invalid(() => pending.ActivateRecoveryAddress(10 + SmartAccountState.ModuleChangeDelayMs - 1));
            var active = pending.ActivateRecoveryAddress(10 + SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(Address(6), active.RecoveryAddress);
            Assert.AreEqual(1UL, active.ConfigurationNonce);
            EmptyIntents(active);
            var removed = active.ProposeRecoveryAddress(UInt160.Zero, 0, true).ActivateRecoveryAddress(SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(UInt160.Zero, removed.RecoveryAddress);
            Assert.AreSequenceEqual(state.Serialize(), pending.CancelRecoveryAddress(true).Serialize());
            Invalid(() => pending.CancelRecoveryAddress(false));
            Invalid(() => state.CancelRecoveryAddress(true));
            Invalid(() => state.ActivateRecoveryAddress(ulong.MaxValue));
            foreach (var bad in new[] { state.CustodyAddress, state.AccountAddress })
                Assert.ThrowsExactly<FormatException>(() => state.ProposeRecoveryAddress(bad, 0, true));
            Invalid(() => pending.ProposeRecoveryAddress(Address(7), 20, true).ActivateRecoveryAddress(10 + SmartAccountState.ModuleChangeDelayMs));
        }

        [TestMethod]
        public void PendingRecoveryCannotBeBypassedThroughConfigurationEpoch()
        {
            var state = Pending();
            foreach (SmartAccountModuleKind kind in Enum.GetValues<SmartAccountModuleKind>())
            {
                Invalid(() => state.ProposeModule(kind, Binding(8), 20, true));
                Invalid(() => state.ActivateModule(kind, ulong.MaxValue));
                _ = state.CancelModule(kind, true);
            }
            Invalid(() => state.ProposeRecoveryAddress(Address(8), 20, true));
            Invalid(() => state.ActivateRecoveryAddress(ulong.MaxValue));
            _ = state.CancelRecoveryAddress(true);
            var cleared = state.CancelRecovery(0, true, false);
            _ = cleared.ActivateModule(SmartAccountModuleKind.Verifier, ulong.MaxValue);
        }

        [TestMethod]
        public void RecoveryBoundaryAndFreezePreservation()
        {
            var baseState = Fresh();
            Invalid(() => baseState.ProposeRecovery(Address(7), 10, false));
            Invalid(() => Fresh(false).ProposeRecovery(Address(7), 10, true));
            Invalid(() => baseState.ExecuteRecovery(ulong.MaxValue));
            Invalid(() => baseState.CancelRecovery(0, true, true));
            foreach (var bad in new[] { UInt160.Zero, baseState.CustodyAddress, baseState.RecoveryAddress, baseState.AccountAddress })
                Assert.ThrowsExactly<FormatException>(() => baseState.ProposeRecovery(bad, 10, true));
            foreach (bool frozen in new[] { false, true })
            {
                var state = frozen ? baseState.Freeze(true) : baseState;
                var pending = state.ProposeRecovery(Address(7), 10, true);
                ulong mature = 10 + SmartAccountState.CustodyRecoveryDelayMs;
                Invalid(() => pending.ExecuteRecovery(mature - 1));
                Invalid(() => pending.CancelRecovery(mature, true, false));
                Invalid(() => pending.CancelRecovery(ulong.MaxValue, true, false));
                Invalid(() => pending.CancelRecovery(0, false, false));
                Assert.AreSequenceEqual(state.Serialize(), pending.CancelRecovery(mature - 1, true, false).Serialize());
                Assert.AreSequenceEqual(state.Serialize(), pending.CancelRecovery(mature, false, true).Serialize());
                var executed = pending.ExecuteRecovery(mature);
                Assert.AreEqual(Address(7), executed.CustodyAddress);
                Assert.AreEqual(state.Status, executed.Status);
                Assert.AreEqual(state.AccountId, executed.AccountId);
                Assert.AreEqual(state.AccountAddress, executed.AccountAddress);
                Assert.AreEqual(state.RecoveryAddress, executed.RecoveryAddress);
                Assert.IsNull(executed.Verifier);
                Assert.IsNull(executed.Hook);
                Assert.AreEqual(state.AuthorityEpoch + 1, executed.AuthorityEpoch);
                Assert.AreEqual(state.ConfigurationNonce + 1, executed.ConfigurationNonce);
                EmptyIntents(executed);
                Invalid(() => pending.ProposeRecovery(Address(8), 20, true).ExecuteRecovery(mature));
            }
            EmptyIntents(Pending().ExecuteRecovery(ulong.MaxValue));
        }

        [TestMethod]
        public void FreezeAndUnfreezeCannotLoseJointAuthority()
        {
            var state = Pending();
            Invalid(() => state.Freeze(false));
            Invalid(() => Fresh(false).Freeze(true));
            Invalid(() => state.Unfreeze(true, true));
            var frozen = state.Freeze(true);
            Assert.AreEqual(SmartAccountStatus.Frozen, frozen.Status);
            Assert.AreEqual(1UL, frozen.ConfigurationNonce);
            EmptyIntents(frozen);
            Invalid(() => frozen.Freeze(true));
            Invalid(() => frozen.Unfreeze(false, true));
            Invalid(() => frozen.Unfreeze(true, false));
            Invalid(() => frozen.ProposeRecoveryAddress(UInt160.Zero, 0, true));
            Invalid(() => frozen.ActivateRecoveryAddress(ulong.MaxValue));
            foreach (SmartAccountModuleKind kind in Enum.GetValues<SmartAccountModuleKind>())
            {
                Invalid(() => frozen.ProposeModule(kind, null, 0, true));
                Invalid(() => frozen.ActivateModule(kind, ulong.MaxValue));
            }
            var active = frozen.ProposeRecovery(Address(7), 0, true).Unfreeze(true, true);
            Assert.AreEqual(SmartAccountStatus.Active, active.Status);
            Assert.AreEqual(2UL, active.ConfigurationNonce);
            EmptyIntents(active);
            var imported = Fresh(false).ToStackItem();
            imported[7] = 1;
            _ = Read(imported).Unfreeze(true, false);
        }

        [TestMethod]
        public void EpochAndTimestampArithmeticNeverWrap()
        {
            var max = AtEpoch(ulong.MaxValue);
            Invalid(() => max.ProposeModule(SmartAccountModuleKind.Verifier, null, 0, true));
            Invalid(() => max.ProposeRecoveryAddress(UInt160.Zero, 0, true));
            Invalid(() => max.ProposeRecovery(Address(7), 0, true));
            Invalid(() => max.Freeze(true));
            var state = Fresh();
            Invalid(() => state.ProposeModule(SmartAccountModuleKind.Hook, null, ulong.MaxValue, true));
            Invalid(() => state.ProposeRecoveryAddress(UInt160.Zero, ulong.MaxValue, true));
            Invalid(() => state.ProposeRecovery(Address(7), ulong.MaxValue, true));
            Assert.AreEqual(ulong.MaxValue, AtEpoch(ulong.MaxValue - 1).Freeze(true).ConfigurationNonce);
            _ = state.ProposeRecovery(Address(7), ulong.MaxValue - SmartAccountState.CustodyRecoveryDelayMs, true).ExecuteRecovery(ulong.MaxValue);
            _ = state.ProposeModule(SmartAccountModuleKind.Verifier, null, ulong.MaxValue - SmartAccountState.ModuleChangeDelayMs, true).ActivateModule(SmartAccountModuleKind.Verifier, ulong.MaxValue);
        }

        [TestMethod]
        public void HashAndStackItemAliasesCannotChangeRecords()
        {
            var address = Address(3);
            var hash = Hash(3);
            var binding = new SmartAccountModuleBinding(address, hash);
            var r = new MemoryReader(Address(9).ToArray()); address.Deserialize(ref r);
            r = new MemoryReader(Hash(9).ToArray()); hash.Deserialize(ref r);
            Assert.AreEqual(Address(3), binding.Contract);
            Assert.AreEqual(Hash(3), binding.CodeHash);
            var state = Pending();
            byte[] before = state.Serialize();
            foreach (var exposed in new[] { state.AccountId, state.AccountAddress, state.CustodyAddress, state.RecoveryAddress, binding.Contract })
            {
                r = new MemoryReader(Address(9).ToArray()); exposed.Deserialize(ref r);
            }
            var exposedHash = binding.CodeHash;
            r = new MemoryReader(Hash(9).ToArray()); exposedHash.Deserialize(ref r);
            var item = state.ToStackItem();
            ((Array)item[9])[0] = Address(9).ToArray();
            item[3] = Address(9).ToArray();
            Assert.AreSequenceEqual(before, state.Serialize());
            Assert.AreEqual(Hash(3), binding.CodeHash);
        }

        [TestMethod]
        public void RecordTypesRangesAndAuthorityRelationsAreStrict()
        {
            foreach (int index in new[] { 0, 7, 8, 13 })
                foreach (StackItem bad in new StackItem[] { Boolean.True, ByteString.Empty, -1, BigInteger.One << 64 })
                { var value = Fresh().ToStackItem(); value[index] = bad; Malformed(value); }
            foreach (int index in new[] { 0, 7 })
            { var value = Fresh().ToStackItem(); value[index] = 3; Malformed(value); }
            foreach (int index in new[] { 1, 2, 3, 4 })
                foreach (StackItem bad in new StackItem[] { new byte[19], new byte[21], 1, new Neo.VM.Types.Buffer(20) })
                { var value = Fresh().ToStackItem(); value[index] = bad; Malformed(value); }
            foreach (int index in new[] { 1, 2, 3 })
            { var value = Fresh().ToStackItem(); value[index] = new byte[20]; Malformed(value); }
            foreach (var (index, bad) in new[] { (3, Fresh().AccountAddress), (4, Address(1)), (4, Fresh().AccountAddress) })
            { var value = Fresh().ToStackItem(); value[index] = bad.ToArray(); Malformed(value); }
            Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Deserialize(Address(9), Fresh().Serialize()));
            var shortRecord = Fresh().ToStackItem(); shortRecord.RemoveAt(12); Malformed(shortRecord);
            var longRecord = Fresh().ToStackItem(); longRecord.Add(StackItem.Null); Malformed(longRecord);
            Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Deserialize(Fresh().AccountId, Encode(new Struct(Fresh().ToStackItem()))));
        }

        [TestMethod]
        public void OptionalRecordsAndPendingEpochsAreStrict()
        {
            foreach (int index in new[] { 5, 6, 9, 10, 11, 12 })
                foreach (StackItem bad in new StackItem[] { new Array(), new Struct(), 0, ByteString.Empty })
                { var value = Pending().ToStackItem(); value[index] = bad; Malformed(value); }
            foreach (int index in new[] { 5, 6 })
            {
                var value = Fresh().ToStackItem(); ((Array)value[index])[0] = new byte[20]; Malformed(value);
                value = Fresh().ToStackItem(); ((Array)value[index])[1] = new byte[31]; Malformed(value);
            }
            foreach (int index in new[] { 9, 10, 11, 12 })
            {
                var value = Pending().ToStackItem(); var pending = (Array)value[index]; pending[^1] = 1; Malformed(value);
                value = Pending().ToStackItem(); pending = (Array)value[index]; pending[^2] = 9; Malformed(value);
                value = Pending().ToStackItem(); pending = (Array)value[index]; pending[^3] = new BigInteger(ulong.MaxValue); Malformed(value);
            }
            var removal = Pending().ToStackItem(); ((Array)removal[10])[1] = Hash(1).ToArray(); Malformed(removal);
            foreach (var bad in new[] { UInt160.Zero, Address(1), Address(2), Fresh().AccountAddress })
            { var value = Pending().ToStackItem(); ((Array)value[12])[0] = bad.ToArray(); Malformed(value); }
            foreach (var bad in new[] { Address(1), Fresh().AccountAddress })
            { var value = Pending().ToStackItem(); ((Array)value[11])[0] = bad.ToArray(); Malformed(value); }
            var missingRecovery = Pending().ToStackItem(); missingRecovery[4] = new byte[20]; Malformed(missingRecovery);
        }

        [TestMethod]
        public void RawDecoderRejectsTruncationTrailingNoncanonicalAndExcessiveInput()
        {
            var state = Pending(); byte[] bytes = state.Serialize();
            for (int i = 0; i < bytes.Length; i++)
                Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Deserialize(state.AccountId, bytes.AsMemory(0, i)));
            foreach (byte[] bad in new byte[][] { [.. bytes, 0], new byte[1025], [0xff], [0x40, 0xfd, 14, 0, .. bytes.AsSpan(2)], [.. bytes.AsSpan(0, 4), 1, 0, .. bytes.AsSpan(5)] })
                Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Deserialize(state.AccountId, bad));
            Array deep = new(); for (int i = 0; i < 70; i++) deep = new([deep]);
            Assert.ThrowsExactly<FormatException>(() => SmartAccountState.Deserialize(state.AccountId, Encode(deep)));
        }

        [TestMethod]
        public void AuthorityEpochIsRecoveryOnlyAndNeverAliasesConfigurationNonce()
        {
            var state = Fresh();
            var configured = state.CommitConfiguration();
            Assert.AreEqual(0UL, configured.AuthorityEpoch);
            Assert.AreEqual(1UL, configured.ConfigurationNonce);
            var recovered = configured.ProposeRecovery(Address(7), 0, true).ExecuteRecovery(SmartAccountState.CustodyRecoveryDelayMs);
            Assert.AreEqual(1UL, recovered.AuthorityEpoch);
            Assert.AreEqual(2UL, recovered.ConfigurationNonce);
            Assert.AreEqual(1UL, recovered.Freeze(true).AuthorityEpoch);
            var invalid = Fresh().ToStackItem(); invalid[13] = 1; Malformed(invalid);
            var legacy = Fresh().ToStackItem(); legacy.RemoveAt(13); legacy[0] = 1; Malformed(legacy);
            var exhausted = AtEpoch(ulong.MaxValue).ToStackItem(); exhausted[13] = new BigInteger(ulong.MaxValue);
            Invalid(() => Read(exhausted).ProposeRecovery(Address(7), 0, true));
        }

        [TestMethod]
        public void InvalidModuleKindIsNeverTreatedAsHook()
        {
            var state = Fresh();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.ProposeModule((SmartAccountModuleKind)2, null, 0, true));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.ActivateModule((SmartAccountModuleKind)2, 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => state.CancelModule((SmartAccountModuleKind)2, true));
        }
    }
}
