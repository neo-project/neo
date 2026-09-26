// Copyright (C) 2015-2026 The Neo Project.
//
// NonFungibleToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Base class for native non-divisible NEP-11 tokens (<see cref="Decimals"/> is always 0).
    /// </summary>
    /// <typeparam name="TState">Per-token state type.</typeparam>
    public abstract class NonFungibleToken<TState> : NativeContract
        where TState : Nep11TokenState, new()
    {
        /// <summary>
        /// Maximum NEP-11 token id length in bytes.
        /// </summary>
        public const int MaxTokenIdLength = 64;

        /// <summary>
        /// Token symbol (e.g. "NNS").
        /// </summary>
        [ContractMethod]
        public abstract string Symbol { get; }

        /// <summary>
        /// Always 0 for non-divisible NEP-11.
        /// </summary>
        [ContractMethod]
        public byte Decimals => 0;

        /// <summary>Storage prefix for total supply.</summary>
        protected abstract byte Prefix_TotalSupply { get; }

        /// <summary>Storage prefix for owner balance (count of tokens).</summary>
        protected abstract byte Prefix_Balance { get; }

        /// <summary>Storage prefix for owner → tokenId index.</summary>
        protected abstract byte Prefix_AccountToken { get; }

        /// <summary>Storage prefix for token state keyed by token key.</summary>
        protected abstract byte Prefix_Token { get; }

        [ContractEvent(0, name: "Transfer",
            "from", ContractParameterType.Hash160,
            "to", ContractParameterType.Hash160,
            "amount", ContractParameterType.Integer,
            "tokenId", ContractParameterType.ByteArray)]
        protected NonFungibleToken() : base() { }

        protected override void OnManifestCompose(IsHardforkEnabledDelegate hfChecker, uint blockHeight, ContractManifest manifest)
        {
            manifest.SupportedStandards = ["NEP-11"];
        }

        /// <summary>
        /// Maps a public token id to the storage key fragment used under <see cref="Prefix_Token"/>.
        /// </summary>
        protected abstract byte[] GetTokenKey(ReadOnlySpan<byte> tokenId);

        /// <summary>
        /// Validates a token id and returns its UTF-8 / raw bytes representation for storage index.
        /// </summary>
        protected virtual byte[] ValidateTokenId(byte[] tokenId)
        {
            ArgumentNullException.ThrowIfNull(tokenId);
            if (tokenId.Length == 0 || tokenId.Length > MaxTokenIdLength)
                throw new ArgumentException($"The tokenId must be 1..{MaxTokenIdLength} bytes.", nameof(tokenId));
            return tokenId;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public virtual BigInteger TotalSupply(IReadOnlyStore snapshot)
        {
            var key = CreateStorageKey(Prefix_TotalSupply);
            return snapshot.TryGet(key, out var item) ? (BigInteger)item : BigInteger.Zero;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public virtual BigInteger BalanceOf(IReadOnlyStore snapshot, UInt160 owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            var key = CreateStorageKey(Prefix_Balance, owner);
            return snapshot.TryGet(key, out var item) ? (BigInteger)item : BigInteger.Zero;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        protected virtual UInt160 OwnerOf(ApplicationEngine engine, byte[] tokenId)
        {
            tokenId = ValidateTokenId(tokenId);
            var state = GetTokenState(engine.SnapshotCache, tokenId)
                ?? throw new InvalidOperationException("The token does not exist.");
            return state.Owner;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        protected virtual Map Properties(ApplicationEngine engine, byte[] tokenId)
        {
            tokenId = ValidateTokenId(tokenId);
            var state = GetTokenState(engine.SnapshotCache, tokenId)
                ?? throw new InvalidOperationException("The token does not exist.");
            return BuildProperties(state);
        }

        /// <summary>
        /// Builds the NEP-11 properties map. Must include required key <c>name</c>.
        /// </summary>
        protected virtual Map BuildProperties(TState state)
        {
            var map = new Map();
            map["name"] = state.Name;
            return map;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        protected virtual IIterator Tokens(IReadOnlyStore snapshot)
        {
            // ValuesOnly + deserialize + field 1 is name/tokenId for NameState; subclasses may override.
            var prefix = CreateStorageKey(Prefix_Token);
            var enumerator = snapshot.Find(prefix).GetEnumerator();
            return new StorageIterator(enumerator, 1, FindOptions.ValuesOnly | FindOptions.DeserializeValues | FindOptions.PickField1);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        protected virtual IIterator TokensOf(IReadOnlyStore snapshot, UInt160 owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            // Account token keys: Prefix_AccountToken || owner || tokenKey → value is raw tokenId bytes
            var prefix = CreateStorageKey(Prefix_AccountToken, owner);
            var enumerator = snapshot.Find(prefix).GetEnumerator();
            // prefix length for RemovePrefix: id(4)+prefix(1)+owner(20) = 25, but StorageIterator uses Key.Key only
            // Key.Key is without contract id: prefix byte + owner + tokenKey. prefixLength 1+20=21
            return new StorageIterator(enumerator, 1 + UInt160.Length, FindOptions.ValuesOnly);
        }

        [ContractMethod(CpuFee = 1 << 17, StorageFee = 50, RequiredCallFlags = CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify)]
        private protected virtual async ContractTask<bool> Transfer(ApplicationEngine engine, UInt160 to, byte[] tokenId, StackItem data)
        {
            ArgumentNullException.ThrowIfNull(to);
            tokenId = ValidateTokenId(tokenId);

            var tokenKey = GetTokenKey(tokenId);
            var storageKey = CreateStorageKey(Prefix_Token, tokenKey);
            var storage = engine.SnapshotCache.GetAndChange(storageKey);
            if (storage is null)
                throw new ArgumentException("The token does not exist.", nameof(tokenId));

            var state = storage.GetInteroperable<TState>();
            var from = state.Owner;
            if (!from.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(from))
                return false;

            if (!from.Equals(to))
            {
                OnTransferring(engine, state, from, to, tokenId);
                state.Owner = to;
                UpdateBalance(engine.SnapshotCache, from, tokenId, tokenKey, -1);
                UpdateBalance(engine.SnapshotCache, to, tokenId, tokenKey, +1);
            }

            await PostTransferAsync(engine, from, to, tokenId, data, true);
            return true;
        }

        /// <summary>
        /// Hook before owner changes on transfer (e.g. clear admin).
        /// </summary>
        protected virtual void OnTransferring(ApplicationEngine engine, TState state, UInt160 from, UInt160 to, byte[] tokenId)
        {
        }

        internal async ContractTask Mint(ApplicationEngine engine, byte[] tokenId, TState state, bool callOnPayment)
        {
            tokenId = ValidateTokenId(tokenId);
            var tokenKey = GetTokenKey(tokenId);
            var storageKey = CreateStorageKey(Prefix_Token, tokenKey);
            if (engine.SnapshotCache.Contains(storageKey))
                throw new InvalidOperationException("The token already exists.");

            engine.SnapshotCache.Add(storageKey, new StorageItem(state));
            UpdateBalance(engine.SnapshotCache, state.Owner, tokenId, tokenKey, +1);

            var supplyKey = CreateStorageKey(Prefix_TotalSupply);
            var supply = engine.SnapshotCache.GetAndChange(supplyKey, () => new StorageItem(BigInteger.Zero));
            supply.Add(1);

            await PostTransferAsync(engine, null, state.Owner, tokenId, StackItem.Null, callOnPayment);
        }

        internal async ContractTask Burn(ApplicationEngine engine, byte[] tokenId)
        {
            tokenId = ValidateTokenId(tokenId);
            var tokenKey = GetTokenKey(tokenId);
            var storageKey = CreateStorageKey(Prefix_Token, tokenKey);
            var storage = engine.SnapshotCache.TryGet(storageKey);
            if (storage is null)
                throw new InvalidOperationException("The token does not exist.");

            var state = storage.GetInteroperable<TState>();
            var owner = state.Owner;
            engine.SnapshotCache.Delete(storageKey);
            UpdateBalance(engine.SnapshotCache, owner, tokenId, tokenKey, -1);

            var supply = engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_TotalSupply))!;
            supply.Add(-1);

            await PostTransferAsync(engine, owner, null, tokenId, StackItem.Null, false);
        }

        protected TState? GetTokenState(IReadOnlyStore snapshot, byte[] tokenId)
        {
            var key = CreateStorageKey(Prefix_Token, GetTokenKey(tokenId));
            return snapshot.TryGet(key, out var item) ? item.GetInteroperableClone<TState>() : null;
        }

        private StorageKey CreateAccountTokenKey(UInt160 owner, byte[] tokenKey)
        {
            var content = new byte[UInt160.Length + tokenKey.Length];
            owner.Serialize(content.AsSpan(0, UInt160.Length));
            tokenKey.CopyTo(content.AsSpan(UInt160.Length));
            return CreateStorageKey(Prefix_AccountToken, content);
        }

        private void UpdateBalance(DataCache snapshot, UInt160 owner, byte[] tokenId, byte[] tokenKey, int delta)
        {
            var balanceKey = CreateStorageKey(Prefix_Balance, owner);
            var accountKey = CreateAccountTokenKey(owner, tokenKey);

            if (delta > 0)
            {
                var balanceItem = snapshot.GetAndChange(balanceKey, () => new StorageItem(BigInteger.Zero));
                balanceItem.Add(delta);
                if (!snapshot.Contains(accountKey))
                    snapshot.Add(accountKey, new StorageItem(tokenId));
            }
            else if (delta < 0)
            {
                var balanceItem = snapshot.GetAndChange(balanceKey)!;
                var balance = (BigInteger)balanceItem;
                if (balance + delta < 0) throw new InvalidOperationException("Insufficient balance.");
                if (balance + delta == 0)
                    snapshot.Delete(balanceKey);
                else
                    balanceItem.Add(delta);
                snapshot.Delete(accountKey);
            }
        }

        private protected virtual async ContractTask PostTransferAsync(
            ApplicationEngine engine, UInt160? from, UInt160? to, byte[] tokenId, StackItem data, bool callOnPayment)
        {
            engine.SendNotification(Hash, "Transfer",
                [
                    from?.ToArray() ?? StackItem.Null,
                    to?.ToArray() ?? StackItem.Null,
                    1,
                    tokenId
                ]);

            if (!callOnPayment || to is null || !ContractManagement.IsContract(engine.SnapshotCache, to))
                return;

            await engine.CallFromNativeContractAsync(Hash, to, "onNEP11Payment",
                from?.ToArray() ?? StackItem.Null, 1, tokenId, data);
        }
    }
}
