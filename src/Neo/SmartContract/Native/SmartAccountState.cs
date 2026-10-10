// Copyright (C) 2015-2026 The Neo Project.
//
// SmartAccountState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.VM.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    internal enum SmartAccountStatus { Active, Frozen }
    internal enum SmartAccountModuleKind { Verifier, Hook }

    /// <summary>A module identity snapshot, not a deployment or ABI validation result.</summary>
    internal sealed class SmartAccountModuleBinding
    {
        private readonly byte[] _contract;
        private readonly byte[] _codeHash;
        internal UInt160 Contract => new(_contract);
        internal UInt256 CodeHash => new(_codeHash);

        internal SmartAccountModuleBinding(UInt160 contract, UInt256 codeHash)
        {
            ArgumentNullException.ThrowIfNull(contract);
            ArgumentNullException.ThrowIfNull(codeHash);
            if (contract == UInt160.Zero || contract == SmartAccountProtocol.ServiceHash || NativeContract.IsNative(contract))
                throw new FormatException("A module must identify a non-native deployed contract.");
            _contract = contract.ToArray();
            _codeHash = codeHash.ToArray();
        }

        internal Array ToStackItem() => new([Contract.ToArray(), CodeHash.ToArray()]);
    }

    /// <summary>
    /// Canonical account storage and immutable lifecycle transitions. The native runtime
    /// must supply witness facts and validate callbacks before persisting a returned state.
    /// This component does not register a contract or perform ledger access.
    /// </summary>
    internal sealed record SmartAccountState
    {
        internal const ulong ModuleChangeDelayMs = 86_400_000;
        internal const ulong CustodyRecoveryDelayMs = 604_800_000;
        private const uint MaximumSize = 1024;
        private const uint MaximumItems = 64;

        private byte[] Identity { get; init; }
        private byte[] Proxy { get; init; }
        private byte[] Custody { get; init; }
        private byte[] Recovery { get; init; }
        internal UInt160 AccountId => new(Identity);
        internal UInt160 AccountAddress => new(Proxy);
        internal UInt160 CustodyAddress => new(Custody);
        internal UInt160 RecoveryAddress => new(Recovery);
        internal SmartAccountModuleBinding? Verifier { get; private init; }
        internal SmartAccountModuleBinding? Hook { get; private init; }
        internal SmartAccountStatus Status { get; private init; }
        internal ulong ConfigurationNonce { get; private init; }
        internal ulong AuthorityEpoch { get; private init; }
        private PendingModule? PendingVerifier { get; init; }
        private PendingModule? PendingHook { get; init; }
        private PendingAddress? PendingRecoveryAddress { get; init; }
        private PendingAddress? PendingRecovery { get; init; }

        private sealed record PendingModule(SmartAccountModuleBinding? Binding, ulong ProposedAt, ulong MatureAt, ulong Epoch)
        {
            internal Array ToStackItem() => new([
                (Binding?.Contract ?? UInt160.Zero).ToArray(),
                (Binding?.CodeHash ?? UInt256.Zero).ToArray(),
                new BigInteger(ProposedAt), new BigInteger(MatureAt), new BigInteger(Epoch)]);
        }

        // No mutable hash instance escapes a state or a pending intent.
        private sealed record PendingAddress(byte[] Address, ulong ProposedAt, ulong MatureAt, ulong Epoch)
        {
            internal Array ToStackItem() => new([
                Address.AsSpan().ToArray(), new BigInteger(ProposedAt), new BigInteger(MatureAt), new BigInteger(Epoch)]);
        }

        private SmartAccountState(UInt160 identity, UInt160 proxy, UInt160 custody, UInt160 recovery)
        {
            Identity = identity.ToArray();
            Proxy = proxy.ToArray();
            Custody = custody.ToArray();
            Recovery = recovery.ToArray();
        }

        internal static SmartAccountState Create(uint network, UInt160 custody, ReadOnlySpan<byte> salt,
            SmartAccountModuleBinding? verifier, SmartAccountModuleBinding? hook, UInt160 recovery, bool custodyAuthorized)
        {
            Require(custodyAuthorized, "Registration requires custody authorization.");
            ArgumentNullException.ThrowIfNull(recovery);
            UInt160 identity = SmartAccountProtocol.GetAccountId(network, custody, salt);
            UInt160 proxy = SmartAccountProtocol.GetAccountAddress(identity);
            ValidateAuthorities(custody, recovery, proxy);
            return new SmartAccountState(identity, proxy, custody, recovery)
            {
                Verifier = verifier,
                Hook = hook,
                Status = SmartAccountStatus.Active
            };
        }

        internal Array ToStackItem() => new([
            (int)SmartAccountProtocol.Version, AccountId.ToArray(), AccountAddress.ToArray(), CustodyAddress.ToArray(), RecoveryAddress.ToArray(),
            Optional(Verifier?.ToStackItem()), Optional(Hook?.ToStackItem()), (int)Status, new BigInteger(ConfigurationNonce),
            Optional(PendingVerifier?.ToStackItem()), Optional(PendingHook?.ToStackItem()),
            Optional(PendingRecoveryAddress?.ToStackItem()), Optional(PendingRecovery?.ToStackItem()), new BigInteger(AuthorityEpoch)]);

        private static StackItem Optional(StackItem? value) => value ?? StackItem.Null;
        internal byte[] Serialize() => BinarySerializer.Serialize(ToStackItem(), MaximumSize, MaximumItems);

        internal static SmartAccountState Deserialize(UInt160 expectedAccountId, ReadOnlyMemory<byte> bytes)
        {
            ArgumentNullException.ThrowIfNull(expectedAccountId);
            if (bytes.Length is 0 or > (int)MaximumSize)
                throw new FormatException("The account record length is invalid.");
            try
            {
                MemoryReader reader = new(bytes);
                Array item = ExactArray(BinarySerializer.Deserialize(ref reader, MaximumSize, MaximumItems), 14);
                if (reader.Position != bytes.Length || Unsigned(item[0]) != SmartAccountProtocol.Version)
                    throw new FormatException("The account record version or byte extent is invalid.");
                UInt160 identity = Address(item[1]);
                UInt160 proxy = Address(item[2]);
                UInt160 custody = Address(item[3]);
                UInt160 recovery = Address(item[4]);
                if (identity == UInt160.Zero || identity != expectedAccountId || proxy != SmartAccountProtocol.GetAccountAddress(identity))
                    throw new FormatException("The record must match its storage key and deterministic proxy.");
                ValidateAuthorities(custody, recovery, proxy);
                ulong status = Unsigned(item[7]);
                if (status > (ulong)SmartAccountStatus.Frozen)
                    throw new FormatException("The account status is not defined.");
                ulong epoch = Unsigned(item[8]);
                var result = new SmartAccountState(identity, proxy, custody, recovery)
                {
                    Verifier = ReadBinding(item[5]),
                    Hook = ReadBinding(item[6]),
                    Status = (SmartAccountStatus)status,
                    ConfigurationNonce = epoch,
                    AuthorityEpoch = Unsigned(item[13]),
                    PendingVerifier = ReadPendingModule(item[9], epoch),
                    PendingHook = ReadPendingModule(item[10], epoch),
                    PendingRecoveryAddress = ReadPendingAddress(item[11], epoch, ModuleChangeDelayMs),
                    PendingRecovery = ReadPendingAddress(item[12], epoch, CustodyRecoveryDelayMs)
                };
                if (result.AuthorityEpoch > result.ConfigurationNonce)
                    throw new FormatException("The authority epoch cannot exceed the configuration counter.");
                if (result.PendingRecoveryAddress is { } rotation)
                    ValidateAuthorities(custody, new UInt160(rotation.Address), proxy);
                if (result.PendingRecovery is { } pending)
                {
                    if (recovery == UInt160.Zero)
                        throw new FormatException("Custody recovery requires a configured authority.");
                    result.ValidateNewCustody(new UInt160(pending.Address));
                }
                if (!bytes.Span.SequenceEqual(result.Serialize()))
                    throw new FormatException("The account record must have a canonical byte encoding.");
                return result;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException)
            {
                throw new FormatException("The account record is malformed.", exception);
            }
        }

        private static Array ExactArray(StackItem item, int count)
        {
            if (item.Type != StackItemType.Array || item is not Array array || array.Count != count)
                throw new FormatException("The account field must be an exact Array of the specified size.");
            return array;
        }

        private static byte[] FixedBytes(StackItem item, int size)
        {
            if (item is not ByteString value || value.Size != size)
                throw new FormatException("The account field must be a fixed-width ByteString.");
            return value.GetSpan().ToArray();
        }

        private static UInt160 Address(StackItem item) => new(FixedBytes(item, UInt160.Length));
        private static UInt256 CodeHash(StackItem item) => new(FixedBytes(item, UInt256.Length));
        private static ulong Unsigned(StackItem item)
        {
            if (item is not Integer value || value.GetInteger() < 0 || value.GetInteger() > ulong.MaxValue)
                throw new FormatException("The account field must be an unsigned 64-bit Integer.");
            return (ulong)value.GetInteger();
        }

        private static SmartAccountModuleBinding? ReadBinding(StackItem item)
        {
            if (item is Null) return null;
            Array record = ExactArray(item, 2);
            return new(Address(record[0]), CodeHash(record[1]));
        }

        private static PendingModule? ReadPendingModule(StackItem item, ulong epoch)
        {
            if (item is Null) return null;
            Array record = ExactArray(item, 5);
            UInt160 address = Address(record[0]);
            UInt256 hash = CodeHash(record[1]);
            SmartAccountModuleBinding? binding = null;
            if (address == UInt160.Zero)
            {
                if (hash != UInt256.Zero) throw new FormatException("Module removal requires a zero code hash.");
            }
            else binding = new(address, hash);
            ulong proposed = Unsigned(record[2]), mature = Unsigned(record[3]), expected = Unsigned(record[4]);
            ValidateIntent(proposed, mature, expected, epoch, ModuleChangeDelayMs);
            return new(binding, proposed, mature, expected);
        }

        private static PendingAddress? ReadPendingAddress(StackItem item, ulong epoch, ulong delay)
        {
            if (item is Null) return null;
            Array record = ExactArray(item, 4);
            byte[] address = FixedBytes(record[0], UInt160.Length);
            ulong proposed = Unsigned(record[1]), mature = Unsigned(record[2]), expected = Unsigned(record[3]);
            ValidateIntent(proposed, mature, expected, epoch, delay);
            return new(address, proposed, mature, expected);
        }

        private static void ValidateIntent(ulong proposed, ulong mature, ulong expected, ulong epoch, ulong delay)
        {
            if (epoch == ulong.MaxValue || expected != epoch || proposed > ulong.MaxValue - delay || mature != proposed + delay)
                throw new FormatException("Pending intents require a usable epoch and the exact fixed delay.");
        }

        private static void ValidateAuthorities(UInt160 custody, UInt160 recovery, UInt160 proxy)
        {
            if (custody == UInt160.Zero || custody == proxy || NativeContract.IsNative(custody) ||
                (recovery != UInt160.Zero && (recovery == custody || recovery == proxy || NativeContract.IsNative(recovery))))
                throw new FormatException("Custody and recovery must have distinct, non-native, non-proxy authority.");
        }

        private void ValidateNewCustody(UInt160 address)
        {
            ArgumentNullException.ThrowIfNull(address);
            if (address == UInt160.Zero || address == CustodyAddress || address == RecoveryAddress || address == AccountAddress || NativeContract.IsNative(address))
                throw new FormatException("New custody must be non-native, nonzero and distinct from current authorities and the proxy.");
        }

        private static void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private void RequireConfiguration()
        {
            Require(Status == SmartAccountStatus.Active, "Configuration requires an Active account.");
            Require(PendingRecovery is null, "Configuration is unavailable during custody recovery.");
        }

        private void RequireRecovery(bool authorized) => Require(RecoveryAddress != UInt160.Zero && authorized, "The configured recovery authority must authorize this transition.");
        private void RequireEpoch() => Require(ConfigurationNonce < ulong.MaxValue, "The configuration counter is exhausted.");
        private ulong Schedule(ulong now, ulong delay)
        {
            RequireEpoch();
            Require(now <= ulong.MaxValue - delay, "The maturity timestamp would overflow.");
            return now + delay;
        }

        private void RequireMature(ulong now, ulong maturity, ulong epoch) => Require(now >= maturity && epoch == ConfigurationNonce, "The intent is immature or stale.");

        internal SmartAccountState CommitConfiguration()
        {
            RequireConfiguration();
            return Advance();
        }

        private SmartAccountState Advance()
        {
            RequireEpoch();
            return this with
            {
                ConfigurationNonce = ConfigurationNonce + 1,
                PendingVerifier = null,
                PendingHook = null,
                PendingRecoveryAddress = null,
                PendingRecovery = null
            };
        }

        private PendingModule? GetPending(SmartAccountModuleKind kind) => kind switch
        {
            SmartAccountModuleKind.Verifier => PendingVerifier,
            SmartAccountModuleKind.Hook => PendingHook,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        internal SmartAccountState ProposeModule(SmartAccountModuleKind kind, SmartAccountModuleBinding? binding, ulong now, bool custodyAuthorized)
        {
            _ = GetPending(kind);
            RequireConfiguration();
            Require(custodyAuthorized, "Module configuration requires custody authorization.");
            var intent = new PendingModule(binding, now, Schedule(now, ModuleChangeDelayMs), ConfigurationNonce);
            return kind == SmartAccountModuleKind.Verifier ? this with { PendingVerifier = intent } : this with { PendingHook = intent };
        }

        internal SmartAccountState ActivateModule(SmartAccountModuleKind kind, ulong now)
        {
            var intent = GetPending(kind);
            RequireConfiguration();
            Require(intent is not null, "No module change is pending.");
            RequireMature(now, intent.MatureAt, intent.Epoch);
            var next = Advance();
            return kind == SmartAccountModuleKind.Verifier ? next with { Verifier = intent.Binding } : next with { Hook = intent.Binding };
        }

        internal SmartAccountState CancelModule(SmartAccountModuleKind kind, bool custodyAuthorized)
        {
            var intent = GetPending(kind);
            Require(custodyAuthorized && intent is not null, "Cancellation requires custody authorization and a pending module change.");
            return kind == SmartAccountModuleKind.Verifier ? this with { PendingVerifier = null } : this with { PendingHook = null };
        }

        internal SmartAccountState ProposeRecoveryAddress(UInt160 address, ulong now, bool custodyAuthorized)
        {
            ArgumentNullException.ThrowIfNull(address);
            RequireConfiguration();
            Require(custodyAuthorized, "Recovery-address configuration requires custody authorization.");
            ValidateAuthorities(CustodyAddress, address, AccountAddress);
            var intent = new PendingAddress(address.ToArray(), now, Schedule(now, ModuleChangeDelayMs), ConfigurationNonce);
            return this with { PendingRecoveryAddress = intent };
        }

        internal SmartAccountState ActivateRecoveryAddress(ulong now)
        {
            RequireConfiguration();
            var intent = PendingRecoveryAddress;
            Require(intent is not null, "No recovery-address change is pending.");
            RequireMature(now, intent.MatureAt, intent.Epoch);
            return Advance() with { Recovery = intent.Address };
        }

        internal SmartAccountState CancelRecoveryAddress(bool custodyAuthorized)
        {
            Require(custodyAuthorized && PendingRecoveryAddress is not null, "Cancellation requires custody authorization and a pending recovery-address change.");
            return this with { PendingRecoveryAddress = null };
        }

        internal SmartAccountState ProposeRecovery(UInt160 address, ulong now, bool recoveryAuthorized)
        {
            RequireRecovery(recoveryAuthorized);
            ValidateNewCustody(address);
            var intent = new PendingAddress(address.ToArray(), now, Schedule(now, CustodyRecoveryDelayMs), ConfigurationNonce);
            return this with { PendingRecovery = intent };
        }

        internal SmartAccountState ExecuteRecovery(ulong now)
        {
            var intent = PendingRecovery;
            Require(intent is not null, "No custody recovery is pending.");
            RequireMature(now, intent.MatureAt, intent.Epoch);
            Require(AuthorityEpoch < ulong.MaxValue, "The authority epoch is exhausted.");
            // Recovery revokes all old module authority without executing untrusted cleanup.
            // Module storage remains unreachable through its former authority-epoch namespace.
            return Advance() with { Custody = intent.Address, AuthorityEpoch = AuthorityEpoch + 1, Verifier = null, Hook = null };
        }

        internal SmartAccountState CancelRecovery(ulong now, bool custodyAuthorized, bool recoveryAuthorized)
        {
            var intent = PendingRecovery;
            Require(intent is not null, "No custody recovery is pending.");
            Require((RecoveryAddress != UInt160.Zero && recoveryAuthorized) || (custodyAuthorized && now < intent.MatureAt), "Custody cancellation expires at recovery maturity.");
            return this with { PendingRecovery = null };
        }

        internal SmartAccountState Freeze(bool recoveryAuthorized)
        {
            Require(Status == SmartAccountStatus.Active, "Only an Active account can be frozen.");
            RequireRecovery(recoveryAuthorized);
            return Advance() with { Status = SmartAccountStatus.Frozen };
        }

        internal SmartAccountState Unfreeze(bool custodyAuthorized, bool recoveryAuthorized)
        {
            Require(Status == SmartAccountStatus.Frozen, "Only a Frozen account can be unfrozen.");
            Require(custodyAuthorized && (RecoveryAddress == UInt160.Zero || recoveryAuthorized), "Unfreeze requires custody and any configured recovery authority.");
            return Advance() with { Status = SmartAccountStatus.Active };
        }
    }
}
