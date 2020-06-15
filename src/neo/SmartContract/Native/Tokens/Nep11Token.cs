#pragma warning disable IDE0060

using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep11Token<TToken, TAccount> : NativeContract
        where TToken : Nep11TokenState, new()
        where TAccount : AccountState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-10", "NEP-11" };
        [ContractMethod(0, CallFlags.None)]
        public abstract string Symbol { get; }
        [ContractMethod(0, CallFlags.None)]
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        private const byte Prefix_TotalSupply = 23;
        private const byte Prefix_OwnerToTokenId = 17;
        private const byte Prefix_TokenIdToOwner = 21;
        private const byte Prefix_TokenId = 19;

        private const int MaxTokenIdLength = 256;

        protected Nep11Token()
        {
            this.Factor = 1;

            Manifest.Features = ContractFeatures.HasStorage;

            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractMethodDescriptor()
                {
                    Name = "Transfer",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "from",
                            Type = ContractParameterType.Hash160
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "to",
                            Type = ContractParameterType.Hash160
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "amount",
                            Type = ContractParameterType.Integer
                        },
                        new ContractParameterDefinition()
                        {
                            Name = "tokenId",
                            Type = ContractParameterType.ByteArray
                        }
                    },
                    ReturnType = ContractParameterType.Boolean
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public BigInteger TotalSupply(ApplicationEngine engine, Array args)
        {
            return TotalSupply(engine.Snapshot);
        }

        public virtual BigInteger TotalSupply(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public BigInteger BalanceOf(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            byte[] tokenId = args[1].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return BigInteger.Zero;
            return BalanceOf(engine.Snapshot, account, tokenId);
        }

        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account, byte[] tokenId)
        {
            UInt160 innerKey = GetInnerKey(tokenId);
            StorageItem storage = snapshot.Storages.TryGet(CreateOwnershipKey(Prefix_OwnerToTokenId, account, innerKey));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<TAccount>().Balance;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public IEnumerator TokensOf(ApplicationEngine engine, Array args)
        {
            UInt160 owner = new UInt160(args[0].GetSpan());
            return TokensOf(engine.Snapshot, owner);
        }

        public virtual IEnumerator TokensOf(StoreView snapshot, UInt160 owner)
        {
            return new CollectionWrapper(snapshot.Storages.Find(CreateStorageKey(Prefix_OwnerToTokenId, owner).ToArray()).Select(p =>
            {
                UInt160 innerKey = new UInt160(p.Key.Key.Skip(1 + UInt160.Length).Take(UInt160.Length).ToArray());
                TToken token = snapshot.Storages.TryGet(CreateTokenKey(innerKey)).GetInteroperable<TToken>();
                return (StackItem)token.Id;
            }).GetEnumerator());
        }

        [ContractMethod(0_01000000, CallFlags.None)]
        public IEnumerator OwnerOf(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) throw new InvalidOperationException("Invalid tokenId");

            var owner = OwnerOf(engine.Snapshot, tokenId);
            if (owner is null) throw new InvalidOperationException("Token doesn't exist");

            var array = new ArrayWrapper(new StackItem[] { owner.ToArray() });
            return array;
        }

        public virtual UInt160 OwnerOf(StoreView snapshot, byte[] tokenId)
        {
            UInt160 innerKey = GetInnerKey(tokenId);
            return snapshot.Storages.Find(CreateStorageKey(Prefix_TokenIdToOwner, innerKey).ToArray()).Select(p =>
            {
                var owner = p.Key.Key.Skip(1 + UInt160.Length).Take(UInt160.Length).ToArray();
                return new UInt160(owner);
            })?.First();
        }

        [ContractMethod(0_08000000, CallFlags.AllowModifyStates)]
        public virtual bool Transfer(ApplicationEngine engine, Array args)
        {
            UInt160 to = new UInt160(args[0].GetSpan());
            byte[] tokenId = args[1].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return Transfer(engine, to, tokenId);
        }

        public virtual bool Transfer(ApplicationEngine engine, UInt160 to, byte[] tokenId)
        {
            UInt160 owner = OwnerOf(engine.Snapshot, tokenId);
            if (owner is null || !engine.CheckWitnessInternal(owner)) return false;

            var snapshot = engine.Snapshot;
            ContractState contract_to = snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;

            UInt160 innerKey = GetInnerKey(tokenId);
            snapshot.Storages.Delete(CreateOwnershipKey(Prefix_OwnerToTokenId, owner, innerKey));
            snapshot.Storages.Delete(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, owner));
            snapshot.Storages.Add(CreateOwnershipKey(Prefix_OwnerToTokenId, to, innerKey), new StorageItem(new TAccount() { Balance = Factor }));
            snapshot.Storages.Add(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, to), new StorageItem(System.Array.Empty<byte>()));

            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", owner.ToArray(), to.ToArray(), Factor, tokenId }));
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowModifyStates)]
        public string Properties(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return null;
            return Properties(engine.Snapshot, tokenId).ToString();
        }

        public abstract JObject Properties(StoreView snapshot, byte[] tokenId);

        internal protected virtual void Mint(ApplicationEngine engine, UInt160 account, TToken token)
        {
            var storages = engine.Snapshot.Storages;
            UInt160 innerKey = GetInnerKey(token.Id);
            StorageKey tokenKey = CreateTokenKey(innerKey);
            if (storages.TryGet(tokenKey) != null) throw new InvalidOperationException("Token already exist");

            storages.Add(tokenKey, new StorageItem(token));
            storages.Add(CreateOwnershipKey(Prefix_OwnerToTokenId, account, innerKey), new StorageItem(new TAccount() { Balance = Factor }));
            storages.Add(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, account), new StorageItem(System.Array.Empty<byte>()));

            IncreaseTotalSupply(engine.Snapshot);

            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), Factor, token.Id }));
        }

        internal protected virtual void Burn(ApplicationEngine engine, byte[] tokenId)
        {
            var storages = engine.Snapshot.Storages;
            UInt160 innerKey = GetInnerKey(tokenId);
            StorageKey tokenKey = CreateTokenKey(innerKey);
            if (!storages.Delete(tokenKey)) throw new InvalidOperationException("Token doesn't exist");

            UInt160 owner = OwnerOf(engine.Snapshot, tokenId);
            storages.Delete(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, owner));
            storages.Delete(CreateOwnershipKey(Prefix_OwnerToTokenId, owner, innerKey));

            DecreaseTotalSupply(engine.Snapshot);

            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", StackItem.Null, StackItem.Null, Factor, tokenId }));
        }

        public virtual UInt160 GetInnerKey(byte[] tokenId)
        {
            return new UInt160(Crypto.Hash160(tokenId));
        }

        private StorageKey CreateOwnershipKey(byte prefix, UInt160 first, UInt160 second)
        {
            byte[] byteSource = new byte[first.Size + second.Size];
            System.Array.Copy(first.ToArray(), 0, byteSource, 0, first.Size);
            System.Array.Copy(second.ToArray(), 0, byteSource, first.Size, second.Size);
            return CreateStorageKey(prefix, byteSource);
        }

        protected StorageKey CreateTokenKey(UInt160 innerKey)
        {
            return CreateStorageKey(Prefix_TokenId, innerKey.ToArray());
        }

        private void IncreaseTotalSupply(StoreView snapshot)
        {
            StorageItem storage_totalSupply = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
            storage_totalSupply.Value = (new BigInteger(storage_totalSupply.Value) + BigInteger.One).ToByteArray();
        }

        private void DecreaseTotalSupply(StoreView snapshot)
        {
            StorageItem storage_totalSupply = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
            BigInteger totalSupply = new BigInteger(storage_totalSupply.Value);
            if (totalSupply.Equals(BigInteger.Zero)) return;
            storage_totalSupply.Value = (totalSupply - BigInteger.One).ToByteArray();
        }
    }
}
