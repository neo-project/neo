// Copyright (C) 2015-2026 The Neo Project.
//
// NameService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable
#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Native Neo Name Service (NNS): non-divisible NEP-11 domains with DNS-like records.
    /// Ported from neo-project/non-native-contracts NameService with committee-gated config
    /// (no contract-wide owner) and optional legacy NNS migration via NEP-11 transfer.
    /// </summary>
    public sealed class NameService : NonFungibleToken<NameState>
    {
        private const int NameMaxLength = 64; // NEP-11 tokenId limit; name is the token id
        private const ulong OneYear = 365ul * TimeSpan.MillisecondsPerDay;
        private const ulong TenYears = OneYear * 10;

        // Storage prefixes aligned with non-native NNS where applicable
        private const byte Prefix_RegisterPrice = 0x11;
        /// <summary>Boolean: when true, public <see cref="Register"/> is paused.</summary>
        private const byte Prefix_RegisterPaused = 0x12;
        private const byte Prefix_Root = 0x20;
        private const byte Prefix_Name = 0x21;
        private const byte Prefix_Record = 0x22;
        private const byte Prefix_LegacyContract = 0x30;

        protected override byte Prefix_TotalSupply => 0x00;
        protected override byte Prefix_Balance => 0x01;
        protected override byte Prefix_AccountToken => 0x02;
        protected override byte Prefix_Token => Prefix_Name;

        public override string Symbol => "NNS";

        public override ImmutableHashSet<Hardfork?> Activations => [Hardfork.HF_Huyao];

        [ContractEvent(1, name: "SetAdmin",
            "name", ContractParameterType.String,
            "oldAdmin", ContractParameterType.Hash160,
            "newAdmin", ContractParameterType.Hash160)]
        [ContractEvent(2, name: "Renew",
            "name", ContractParameterType.String,
            "oldExpiration", ContractParameterType.Integer,
            "newExpiration", ContractParameterType.Integer)]
        internal NameService() : base() { }

        protected override void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest)
        {
            manifest.SupportedStandards = ["NEP-11"];
        }

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == ActiveIn)
            {
                // Default price list (same as non-native); pre-registration deferred for maintainer feedback.
                long[] priceList =
                [
                    2_00000000,   // default for other lengths
                    -1,           // length 1 closed
                    -1,           // length 2 closed
                    200_00000000, // length 3
                    70_00000000,  // length 4
                ];
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_RegisterPrice), new StorageItem(SerializePriceList(priceList)));
                // Public register starts paused so legacy migration can proceed without open races.
                // Committee unpauses when ready for open registration.
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_RegisterPaused), new StorageItem(BigInteger.One));
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_Root, Utility.StrictUTF8.GetBytes("neo")), new StorageItem(0));
                engine.SnapshotCache.Add(CreateStorageKey(Prefix_TotalSupply), new StorageItem(BigInteger.Zero));
            }
            return ContractTask.CompletedTask;
        }

        protected override byte[] GetTokenKey(ReadOnlySpan<byte> tokenId) =>
            tokenId.RIPEMD160();

        protected override byte[] ValidateTokenId(byte[] tokenId)
        {
            tokenId = base.ValidateTokenId(tokenId);
            // Token id is the domain name StrictUTF-8 bytes
            var name = Utility.StrictUTF8.GetString(tokenId);
            if (SplitAndCheck(name, false) is null)
                throw new FormatException("The format of the name is incorrect.");
            return tokenId;
        }

        protected override Map BuildProperties(NameState state)
        {
            var map = new Map();
            map["name"] = state.Name;
            map["expiration"] = state.Expiration;
            map["admin"] = state.Admin is null ? StackItem.Null : state.Admin.ToArray();
            // TODO: Find a CDN site for the NNS logo image
            map["image"] = "https://neo.link/_next/static/media/nnslogo.1314e9b5.svg";
            return map;
        }

        protected override UInt160 OwnerOf(ApplicationEngine engine, byte[] tokenId)
        {
            tokenId = ValidateTokenId(tokenId);
            var state = GetTokenState(engine.SnapshotCache, tokenId)
                ?? throw new InvalidOperationException("The token does not exist.");
            state.EnsureNotExpired(engine.GetTime());
            return state.Owner;
        }

        protected override Map Properties(ApplicationEngine engine, byte[] tokenId)
        {
            tokenId = ValidateTokenId(tokenId);
            var state = GetTokenState(engine.SnapshotCache, tokenId)
                ?? throw new InvalidOperationException("The token does not exist.");
            state.EnsureNotExpired(engine.GetTime());
            return BuildProperties(state);
        }

        private protected override async ContractTask<bool> Transfer(ApplicationEngine engine, UInt160 to, byte[] tokenId, StackItem data)
        {
            tokenId = ValidateTokenId(tokenId);
            var state = GetTokenState(engine.SnapshotCache, tokenId)
                ?? throw new ArgumentException("The token does not exist.", nameof(tokenId));
            state.EnsureNotExpired(engine.GetTime());
            return await base.Transfer(engine, to, tokenId, data);
        }

        protected override void OnTransferring(ApplicationEngine engine, NameState state, UInt160 from, UInt160 to, byte[] tokenId)
        {
            state.Admin = null;
        }

        #region Committee config

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void AddRoot(ApplicationEngine engine, string root)
        {
            AssertCommittee(engine);
            if (!CheckFragment(root, true))
                throw new FormatException("The format of the root is incorrect.");
            var key = CreateStorageKey(Prefix_Root, Utility.StrictUTF8.GetBytes(root));
            if (engine.SnapshotCache.Contains(key))
                throw new InvalidOperationException("The root already exists.");
            engine.SnapshotCache.Add(key, new StorageItem(0));
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private IIterator Roots(IReadOnlyStore snapshot)
        {
            var prefix = CreateStorageKey(Prefix_Root);
            var enumerator = snapshot.Find(prefix).GetEnumerator();
            return new StorageIterator(enumerator, 1, FindOptions.KeysOnly | FindOptions.RemovePrefix);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetPrice(ApplicationEngine engine, Array priceList)
        {
            AssertCommittee(engine);
            if (priceList.Count == 0)
                throw new ArgumentException("The price list must contain at least 1 item.");
            var prices = new long[priceList.Count];
            for (var i = 0; i < priceList.Count; i++)
            {
                var price = (long)priceList[i].GetInteger();
                if (price < -1 || price > 10000_00000000)
                    throw new ArgumentException("The price is out of range.");
                prices[i] = price;
            }
            if (prices[0] == -1)
                throw new ArgumentException("The price is out of range.");
            var priceItem = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_RegisterPrice),
                () => new StorageItem(SerializePriceList([2_00000000])));
            priceItem!.Value = SerializePriceList(prices);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private long GetPrice(IReadOnlyStore snapshot, byte length)
        {
            if (length == 0) throw new ArgumentException("Length cannot be 0.");
            var prices = GetPriceList(snapshot);
            if (length >= prices.Length) length = 0;
            return prices[length];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void AddLegacyContract(ApplicationEngine engine, UInt160 contractHash)
        {
            AssertCommittee(engine);
            ArgumentNullException.ThrowIfNull(contractHash);
            var key = CreateStorageKey(Prefix_LegacyContract, contractHash);
            if (engine.SnapshotCache.Contains(key))
                throw new InvalidOperationException("Legacy contract already registered.");
            engine.SnapshotCache.Add(key, new StorageItem(1));
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void RemoveLegacyContract(ApplicationEngine engine, UInt160 contractHash)
        {
            AssertCommittee(engine);
            var key = CreateStorageKey(Prefix_LegacyContract, contractHash);
            if (!engine.SnapshotCache.Contains(key))
                throw new InvalidOperationException("Legacy contract not found.");
            engine.SnapshotCache.Delete(key);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private bool IsLegacyContract(IReadOnlyStore snapshot, UInt160 contractHash)
        {
            if (contractHash is null) return false;
            return snapshot.Contains(CreateStorageKey(Prefix_LegacyContract, contractHash));
        }

        /// <summary>
        /// Pauses or unpauses public name registration. Committee only.
        /// When paused, <c>register</c> is closed so legacy holders can migrate via
        /// <c>onNEP11Payment</c> without open-registration races.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetRegisterPaused(ApplicationEngine engine, bool paused)
        {
            AssertCommittee(engine);
            var item = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_RegisterPaused),
                () => new StorageItem(BigInteger.Zero));
            item!.Set(paused ? BigInteger.One : BigInteger.Zero);
        }

        /// <summary>
        /// Whether public <c>register</c> is paused.
        /// </summary>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private bool IsRegisterPaused(IReadOnlyStore snapshot)
        {
            var key = CreateStorageKey(Prefix_RegisterPaused);
            if (!snapshot.TryGet(key, out var item)) return false;
            return (BigInteger)item != BigInteger.Zero;
        }

        #endregion

        #region Registry

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private bool IsAvailable(ApplicationEngine engine, string name)
        {
            var fragments = SplitAndCheck(name, false)
                ?? throw new FormatException("The format of the name is incorrect.");
            if (!engine.SnapshotCache.Contains(CreateStorageKey(Prefix_Root, Utility.StrictUTF8.GetBytes(fragments[^1]))))
                throw new InvalidOperationException("The root does not exist.");
            // While registration is paused, names are not available for public registration.
            if (IsRegisterPaused(engine.SnapshotCache)) return false;
            var price = GetPrice(engine.SnapshotCache, (byte)fragments[0].Length);
            if (price < 0) return false;
            var tokenId = Utility.StrictUTF8.GetBytes(name);
            if (tokenId.Length > MaxTokenIdLength) return false;
            var state = GetTokenState(engine.SnapshotCache, tokenId);
            if (state is null) return true;
            return engine.GetTime() >= state.Expiration;
        }

        [ContractMethod(CpuFee = 1 << 17, StorageFee = 100, RequiredCallFlags = CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify)]
        private async ContractTask<bool> Register(ApplicationEngine engine, string name, UInt160 owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (IsRegisterPaused(engine.SnapshotCache))
                throw new InvalidOperationException("Public registration is paused.");
            var fragments = SplitAndCheck(name, false)
                ?? throw new FormatException("The format of the name is incorrect.");
            if (!engine.SnapshotCache.Contains(CreateStorageKey(Prefix_Root, Utility.StrictUTF8.GetBytes(fragments[^1]))))
                throw new InvalidOperationException("The root does not exist.");
            if (!owner.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(owner))
                throw new InvalidOperationException("No authorization.");

            var price = GetPrice(engine.SnapshotCache, (byte)fragments[0].Length);
            if (price < 0)
                AssertCommittee(engine);
            else
                engine.AddFee(price, true);

            var tokenId = Utility.StrictUTF8.GetBytes(name);
            if (tokenId.Length > MaxTokenIdLength)
                throw new FormatException("The format of the name is incorrect.");

            var existing = GetTokenState(engine.SnapshotCache, tokenId);
            if (existing is not null)
            {
                if (engine.GetTime() < existing.Expiration) return false;
                await Burn(engine, tokenId);
                ClearRecords(engine.SnapshotCache, tokenId);
            }

            var state = new NameState
            {
                Owner = owner,
                Name = name,
                Expiration = engine.GetTime() + OneYear,
                Admin = null
            };
            await Mint(engine, tokenId, state, false);
            return true;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private ulong Renew(ApplicationEngine engine, string name) =>
            Renew(engine, name, 1);

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private ulong Renew(ApplicationEngine engine, string name, byte years)
        {
            if (years < 1 || years > 10)
                throw new ArgumentException("The argument `years` is out of range.");
            var fragments = SplitAndCheck(name, false)
                ?? throw new FormatException("The format of the name is incorrect.");
            var price = GetPrice(engine.SnapshotCache, (byte)fragments[0].Length);
            if (price < 0)
                AssertCommittee(engine);
            else
                engine.AddFee(price * years, true);

            var tokenId = Utility.StrictUTF8.GetBytes(name);
            var tokenKey = GetTokenKey(tokenId);
            var storageKey = CreateStorageKey(Prefix_Name, tokenKey);
            var storage = engine.SnapshotCache.GetAndChange(storageKey)
                ?? throw new InvalidOperationException("The token does not exist.");
            var token = storage.GetInteroperable<NameState>();
            token.EnsureNotExpired(engine.GetTime());
            var oldExpiration = token.Expiration;
            token.Expiration += OneYear * years;
            if (token.Expiration > engine.GetTime() + TenYears)
                throw new ArgumentException("You can't renew a domain name for more than 10 years in total.");

            engine.SendNotification(Hash, "Renew", new Array() { name, oldExpiration, token.Expiration });
            return token.Expiration;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify)]
        private void SetAdmin(ApplicationEngine engine, string name, UInt160? admin)
        {
            if (admin is not null && !admin.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(admin))
                throw new InvalidOperationException("No authorization.");

            var tokenId = Utility.StrictUTF8.GetBytes(name);
            var storageKey = CreateStorageKey(Prefix_Name, GetTokenKey(tokenId));
            var storage = engine.SnapshotCache.GetAndChange(storageKey)
                ?? throw new InvalidOperationException("The token does not exist.");
            var token = storage.GetInteroperable<NameState>();
            token.EnsureNotExpired(engine.GetTime());
            if (!engine.CheckWitnessInternal(token.Owner))
                throw new InvalidOperationException("No authorization.");

            var old = token.Admin;
            token.Admin = admin;
            engine.SendNotification(Hash, "SetAdmin", new Array()
            {
                name,
                old?.ToArray() ?? StackItem.Null,
                admin?.ToArray() ?? StackItem.Null
            });
        }

        #endregion

        #region Records

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetRecord(ApplicationEngine engine, string name, byte type, string data)
        {
            var recordType = (RecordType)type;
            ValidateRecordData(name, recordType, data);
            var (tokenId, tokenKey) = ResolveTokenFromRecordName(engine.SnapshotCache, name, true);
            var storage = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Name, tokenKey))
                ?? throw new InvalidOperationException("The token does not exist.");
            var token = storage.GetInteroperable<NameState>();
            token.EnsureNotExpired(engine.GetTime());
            token.CheckAdmin(engine);

            var recordKey = GetRecordKey(tokenKey, name, recordType);
            var item = engine.SnapshotCache.GetAndChange(recordKey, () => new StorageItem(new RecordState()));
            var record = item.GetInteroperable<RecordState>();
            record.Name = name;
            record.Type = recordType;
            record.Data = data;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private string? GetRecord(ApplicationEngine engine, string name, byte type)
        {
            var recordType = (RecordType)type;
            var (_, tokenKey) = ResolveTokenFromRecordName(engine.SnapshotCache, name, true);
            var storage = engine.SnapshotCache.TryGet(CreateStorageKey(Prefix_Name, tokenKey))
                ?? throw new InvalidOperationException("The token does not exist.");
            var token = storage.GetInteroperableClone<NameState>();
            token.EnsureNotExpired(engine.GetTime());
            var recordKey = GetRecordKey(tokenKey, name, recordType);
            if (!engine.SnapshotCache.TryGet(recordKey, out var item)) return null;
            return item.GetInteroperableClone<RecordState>().Data;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private IIterator GetAllRecords(ApplicationEngine engine, string name)
        {
            var tokenId = Utility.StrictUTF8.GetBytes(name);
            var tokenKey = GetTokenKey(tokenId);
            var storage = engine.SnapshotCache.TryGet(CreateStorageKey(Prefix_Name, tokenKey))
                ?? throw new InvalidOperationException("The token does not exist.");
            var token = storage.GetInteroperableClone<NameState>();
            token.EnsureNotExpired(engine.GetTime());
            var prefix = CreateStorageKey(Prefix_Record, tokenKey);
            var enumerator = engine.SnapshotCache.Find(prefix).GetEnumerator();
            return new StorageIterator(enumerator, 1 + tokenKey.Length, FindOptions.ValuesOnly | FindOptions.DeserializeValues);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void DeleteRecord(ApplicationEngine engine, string name, byte type)
        {
            var recordType = (RecordType)type;
            var (_, tokenKey) = ResolveTokenFromRecordName(engine.SnapshotCache, name, true);
            var storage = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Name, tokenKey))
                ?? throw new InvalidOperationException("The token does not exist.");
            var token = storage.GetInteroperable<NameState>();
            token.EnsureNotExpired(engine.GetTime());
            token.CheckAdmin(engine);
            engine.SnapshotCache.Delete(GetRecordKey(tokenKey, name, recordType));
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private string? Resolve(ApplicationEngine engine, string name, byte type) =>
            Resolve(engine, name, (RecordType)type, 2);

        private string? Resolve(ApplicationEngine engine, string name, RecordType type, int redirect)
        {
            if (redirect < 0) throw new InvalidOperationException("Too many redirections.");
            if (name.Length == 0) throw new InvalidOperationException("Invalid name.");
            if (name[^1] == '.') name = name[..^1];

            string? cname = null;
            foreach (var (rt, data) in GetRecords(engine, name))
            {
                if (rt == type) return data;
                if (rt == RecordType.CNAME) cname = data;
            }
            return cname is null ? null : Resolve(engine, cname, type, redirect - 1);
        }

        #endregion

        #region Migration: accept legacy NNS NEP-11 transfer

        /// <summary>
        /// Accepts NEP-11 payment from a committee-registered legacy NNS contract.
        /// Caller must be the legacy contract (transfer of old name token to this native).
        /// Binds the domain under native NNS to <paramref name="from"/> (previous owner).
        /// </summary>
        [ContractMethod(CpuFee = 1 << 17, StorageFee = 100, RequiredCallFlags = CallFlags.States | CallFlags.AllowNotify, Name = "onNEP11Payment")]
        private async ContractTask OnNEP11Payment(ApplicationEngine engine, UInt160 from, BigInteger amount, byte[] tokenId, StackItem data)
        {
            var caller = engine.CallingScriptHash
                ?? throw new InvalidOperationException("Missing calling script hash.");
            if (!IsLegacyContract(engine.SnapshotCache, caller))
                throw new InvalidOperationException("Only committee-registered legacy NNS contracts can migrate names.");
            ArgumentNullException.ThrowIfNull(from);
            if (amount.Sign <= 0)
                throw new ArgumentException("Invalid amount.", nameof(amount));

            tokenId = ValidateTokenId(tokenId);
            var name = Utility.StrictUTF8.GetString(tokenId);

            // If already native-owned and not expired, reject; if expired, reclaim.
            var existing = GetTokenState(engine.SnapshotCache, tokenId);
            if (existing is not null)
            {
                if (engine.GetTime() < existing.Expiration)
                    throw new InvalidOperationException("Name already registered on native NameService.");
                await Burn(engine, tokenId);
                ClearRecords(engine.SnapshotCache, tokenId);
            }

            var state = new NameState
            {
                Owner = from,
                Name = name,
                Expiration = engine.GetTime() + OneYear,
                Admin = null
            };
            await Mint(engine, tokenId, state, false);
        }

        #endregion

        #region Helpers

        private long[] GetPriceList(IReadOnlyStore snapshot)
        {
            var item = snapshot[CreateStorageKey(Prefix_RegisterPrice)];
            return DeserializePriceList(item.Value.Span);
        }

        private static byte[] SerializePriceList(long[] prices)
        {
            var pricesLength = prices.Length;
            var prefixLengthBytes = MemoryMarshal.CreateSpan(ref Unsafe.As<int, byte>(ref pricesLength), sizeof(int));
            var pricesArrayBytes = MemoryMarshal.CreateSpan(ref Unsafe.As<long, byte>(ref prices[0]), prices.Length * sizeof(long));
            return [.. prefixLengthBytes, .. pricesArrayBytes];
        }


        private static long[] DeserializePriceList(ReadOnlySpan<byte> data)
        {
            var pricesLength = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(data));
            var pricesSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(data[sizeof(int)..])), pricesLength);
            return [.. pricesSpan];
        }

        private StorageKey GetRecordKey(byte[] tokenKey, string name, RecordType type)
        {
            var nameKey = Utility.StrictUTF8.GetBytes(name).AsSpan().RIPEMD160();
            var content = new byte[tokenKey.Length + nameKey.Length + 1];
            tokenKey.CopyTo(content.AsSpan(0));
            nameKey.CopyTo(content.AsSpan(tokenKey.Length));
            content[^1] = (byte)type;
            return CreateStorageKey(Prefix_Record, content);
        }

        private (byte[] tokenId, byte[] tokenKey) ResolveTokenFromRecordName(IReadOnlyStore snapshot, string name, bool allowMultiple)
        {
            var fragments = SplitAndCheck(name, allowMultiple)
                ?? throw new FormatException("The format of the name is incorrect.");
            // second-level domain: last two labels
            var tokenName = name[^(fragments[^2].Length + fragments[^1].Length + 1)..];
            var tokenId = Utility.StrictUTF8.GetBytes(tokenName);
            if (tokenId.Length > MaxTokenIdLength)
                throw new FormatException("The format of the name is incorrect.");
            return (tokenId, GetTokenKey(tokenId));
        }

        private void ClearRecords(DataCache snapshot, byte[] tokenId)
        {
            var tokenKey = GetTokenKey(tokenId);
            var prefix = CreateStorageKey(Prefix_Record, tokenKey);
            foreach (var (key, _) in snapshot.Find(prefix).ToArray())
                snapshot.Delete(key);
        }

        private IEnumerable<(RecordType type, string data)> GetRecords(ApplicationEngine engine, string name)
        {
            var (_, tokenKey) = ResolveTokenFromRecordName(engine.SnapshotCache, name, true);
            var storage = engine.SnapshotCache.TryGet(CreateStorageKey(Prefix_Name, tokenKey))
                ?? throw new InvalidOperationException("The token does not exist.");
            storage.GetInteroperableClone<NameState>().EnsureNotExpired(engine.GetTime());

            var nameKey = Utility.StrictUTF8.GetBytes(name).AsSpan().RIPEMD160();
            var content = new byte[tokenKey.Length + nameKey.Length];
            tokenKey.CopyTo(content.AsSpan(0));
            nameKey.CopyTo(content.AsSpan(tokenKey.Length));
            var prefix = CreateStorageKey(Prefix_Record, content);
            foreach (var (_, value) in engine.SnapshotCache.Find(prefix))
            {
                var record = value.GetInteroperableClone<RecordState>();
                yield return (record.Type, record.Data);
            }
        }

        private static void ValidateRecordData(string name, RecordType type, string data)
        {
            _ = SplitAndCheck(name, true) ?? throw new FormatException("The format of the name is incorrect.");
            switch (type)
            {
                case RecordType.A:
                    if (!CheckIPv4(data)) throw new FormatException("The format of the A record is incorrect.");
                    break;
                case RecordType.CNAME:
                    if (SplitAndCheck(data, true) is null) throw new FormatException("The format of the CNAME record is incorrect.");
                    break;
                case RecordType.TXT:
                    if (data.Length > 255) throw new FormatException("The format of the TXT record is incorrect.");
                    break;
                case RecordType.AAAA:
                    if (!CheckIPv6(data)) throw new FormatException("The format of the AAAA record is incorrect.");
                    break;
                default:
                    throw new InvalidOperationException("The record type is not supported.");
            }
        }

        private static bool CheckFragment(string root, bool isRoot)
        {
            var maxLength = isRoot ? 16 : 63;
            if (root.Length == 0 || root.Length > maxLength) return false;
            var c = root[0];
            if (isRoot)
            {
                if (!IsAlpha(c)) return false;
            }
            else
            {
                if (!IsAlphaNum(c)) return false;
            }
            if (root.Length == 1) return true;
            for (var i = 1; i < root.Length - 1; i++)
            {
                c = root[i];
                if (!(IsAlphaNum(c) || c == '-')) return false;
            }
            return IsAlphaNum(root[^1]);
        }

        private static bool IsAlpha(char c) =>
            c is >= 'a' and <= 'z';

        private static bool IsAlphaNum(char c) =>
            IsAlpha(c) || c is >= '0' and <= '9';

        private static string[]? SplitAndCheck(string name, bool allowMultipleFragments)
        {
            var length = name.Length;
            if (length < 3 || length > NameMaxLength) return null;
            var fragments = name.Split('.');
            length = fragments.Length;
            if (length < 2 || length > 8) return null;
            if (length > 2 && !allowMultipleFragments) return null;
            for (var i = 0; i < length; i++)
                if (!CheckFragment(fragments[i], i == length - 1))
                    return null;
            return fragments;
        }

        private static bool CheckIPv4(string ipv4)
        {
            var length = ipv4.Length;
            if (length < 7 || length > 15) return false;
            var fragments = ipv4.Split('.');
            if (fragments.Length != 4) return false;
            var numbers = new byte[4];
            for (var i = 0; i < 4; i++)
            {
                var fragment = fragments[i];
                if (fragment.Length == 0) return false;
                if (!byte.TryParse(fragment, out var number)) return false;
                if (number > 0 && fragment[0] == '0') return false;
                if (number == 0 && fragment.Length > 1) return false;
                numbers[i] = number;
            }
            switch (numbers[0])
            {
                case 0:
                case 10:
                case 100 when numbers[1] >= 64 && numbers[1] <= 127:
                case 127:
                case 169 when numbers[1] == 254:
                case 172 when numbers[1] >= 16 && numbers[1] <= 31:
                case 192 when numbers[1] == 0 && numbers[2] == 0:
                case 192 when numbers[1] == 0 && numbers[2] == 2:
                case 192 when numbers[1] == 88 && numbers[2] == 99:
                case 192 when numbers[1] == 168:
                case 198 when numbers[1] >= 18 && numbers[1] <= 19:
                case 198 when numbers[1] == 51 && numbers[2] == 100:
                case 203 when numbers[1] == 0 && numbers[2] == 113:
                case >= 224:
                    return false;
            }
            return numbers[3] is not (0 or 255);
        }

        private static bool CheckIPv6(string ipv6)
        {
            var length = ipv6.Length;
            if (length < 2 || length > 39) return false;
            var fragments = ipv6.Split(':');
            length = fragments.Length;
            if (length < 3 || length > 8) return false;
            var numbers = new ushort[8];
            var isCompressed = false;
            for (var i = 0; i < length; i++)
            {
                var fragment = fragments[i];
                if (fragment.Length == 0)
                {
                    if (i == 0)
                    {
                        if (fragments[1].Length != 0) return false;
                        numbers[0] = 0;
                    }
                    else if (i == length - 1)
                    {
                        if (fragments[i - 1].Length != 0) return false;
                        numbers[7] = 0;
                    }
                    else
                    {
                        if (isCompressed) return false;
                        isCompressed = true;
                        var endIndex = 9 - length + i;
                        for (var j = i; j < endIndex; j++)
                            numbers[j] = 0;
                    }
                }
                else
                {
                    if (fragment.Length > 4) return false;
                    var index = isCompressed ? i + 8 - length : i;
                    if (!ushort.TryParse(fragment, System.Globalization.NumberStyles.HexNumber, null, out numbers[index]))
                        return false;
                }
            }
            if (length < 8 && !isCompressed) return false;
            var number = numbers[0];
            if (number < 0x2000 || number == 0x2002 || number == 0x3ffe || number > 0x3fff)
                return false;
            if (number == 0x2001)
            {
                number = numbers[1];
                if (number < 0x200 || number == 0xdb8) return false;
            }
            return true;
        }

        #endregion
    }
}

#nullable disable
