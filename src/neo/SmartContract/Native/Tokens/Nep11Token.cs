#pragma warning disable IDE0060

using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Enumerators;
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
        public abstract string Symbol { get; }
        public BigInteger Factor { get; }

        public const byte Decimals = 0;

        private const byte Prefix_TotalSupply = 20;
        private const byte Prefix_OwnerToTokenId = 21;
        private const byte Prefix_TokenIdToOwner = 22;
        private const byte Prefix_TokenId = 23;

        private const int MaxTokenIdLength = 256;

        protected Nep11Token()
        {
            this.Factor = BigInteger.Pow(10, Decimals);

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

        [ContractMethod(0, ContractParameterType.String, CallFlags.None, Name = "name")]
        protected StackItem NameMethod(ApplicationEngine engine, Array args)
        {
            return Name;
        }

        [ContractMethod(0, ContractParameterType.String, CallFlags.None, Name = "symbol")]
        protected StackItem SymbolMethod(ApplicationEngine engine, Array args)
        {
            return Symbol;
        }

        [ContractMethod(0, ContractParameterType.Integer, CallFlags.None, Name = "decimals")]
        protected StackItem DecimalsMethod(ApplicationEngine engine, Array args)
        {
            return (uint)Decimals;
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, CallFlags.AllowStates)]
        protected StackItem TotalSupply(ApplicationEngine engine, Array args)
        {
            return TotalSupply(engine.Snapshot);
        }

        public virtual BigInteger TotalSupply(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_TotalSupply));
            if (storage is null) return BigInteger.Zero;
            return new BigInteger(storage.Value);
        }

        [ContractMethod(0_01000000, ContractParameterType.Integer, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.ByteArray }, ParameterNames = new[] { "account", "tokenId" })]
        protected StackItem BalanceOf(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            byte[] tokenId = args[1].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return BalanceOf(engine.Snapshot, account, tokenId);
        }

        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account, byte[] tokenId)
        {
            UInt160 innerKey = GetInnerKey(tokenId);
            StorageItem storage = snapshot.Storages.TryGet(CreateOwnershipKey(Prefix_OwnerToTokenId, account, innerKey));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<TAccount>().Balance;
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        public StackItem TokensOf(ApplicationEngine engine, Array args)
        {
            UInt160 owner = new UInt160(args[0].GetSpan());
            return new InteropInterface(TokensOf(engine.Snapshot, owner));
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

        [ContractMethod(0_01000000, ContractParameterType.InteropInterface, CallFlags.None, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "tokenId" })]
        public StackItem OwnerOf(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return new InteropInterface(OwnerOf(engine.Snapshot, tokenId));
        }

        public virtual IEnumerator OwnerOf(StoreView snapshot, byte[] tokenId)
        {
            UInt160 innerKey = GetInnerKey(tokenId);
            return new CollectionWrapper(snapshot.Storages.Find(CreateStorageKey(Prefix_TokenIdToOwner, innerKey).ToArray()).Select(p =>
            {
                var owner = p.Key.Key.Skip(1 + UInt160.Length).Take(UInt160.Length).ToArray();
                return (StackItem)owner;
            }).GetEnumerator());
        }

        [ContractMethod(0_08000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.ByteArray }, ParameterNames = new[] { "to", "tokenId" })]
        public virtual StackItem Transfer(ApplicationEngine engine, Array args)
        {
            UInt160 to = new UInt160(args[0].GetSpan());
            byte[] tokenId = args[1].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return Transfer(engine, to, tokenId);
        }

        public virtual bool Transfer(ApplicationEngine engine, UInt160 to, byte[] tokenId)
        {
            IEnumerator enumerator = OwnerOf(engine.Snapshot, tokenId);
            if (!enumerator.Next()) return false;
            UInt160 owner = new UInt160(enumerator.Value().GetSpan().ToArray());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, owner)) return false;

            var snapshot = engine.Snapshot;
            ContractState contract_to = snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;

            UInt160 innerKey = GetInnerKey(tokenId);
            snapshot.Storages.Delete(CreateOwnershipKey(Prefix_OwnerToTokenId, owner, innerKey));
            snapshot.Storages.Delete(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, owner));
            snapshot.Storages.Add(CreateOwnershipKey(Prefix_OwnerToTokenId, to, innerKey), new StorageItem(new TAccount() { Balance = Factor }));
            snapshot.Storages.Add(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, to), new StorageItem(new byte[0]));

            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", owner.ToArray(), to.ToArray(), Factor, tokenId }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "tokenId" })]
        protected StackItem Properties(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
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
            storages.Add(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, account), new StorageItem(new byte[0]));

            IncreaseTotalSupply(engine.Snapshot);

            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), Factor, token.Id }));
        }

        internal protected virtual void Burn(ApplicationEngine engine, byte[] tokenId)
        {
            var storages = engine.Snapshot.Storages;
            UInt160 innerKey = GetInnerKey(tokenId);
            StorageKey tokenKey = CreateTokenKey(innerKey);
            if (!storages.Delete(tokenKey)) throw new InvalidOperationException("Token doesn't exist");

            IEnumerator enumerator = OwnerOf(engine.Snapshot, tokenId);
            if (enumerator.Next())
            {
                UInt160 owner = new UInt160(enumerator.Value().GetSpan().ToArray());
                storages.Delete(CreateOwnershipKey(Prefix_TokenIdToOwner, innerKey, owner));
                storages.Delete(CreateOwnershipKey(Prefix_OwnerToTokenId, owner, innerKey));
            }

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
