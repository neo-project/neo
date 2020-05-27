#pragma warning disable IDE0060

using Akka.Configuration.Hocon;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
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
    public abstract class Nep11Token<TState, UState> : NativeContract
        where TState : Nep11TokenState, new()
        where UState : AccountState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-11", "NEP-10" };
        public abstract string Symbol { get; }
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        private const byte Prefix_TotalSupply = 20;
        private const byte Prefix_Ownership = 21;
        private const byte Prefix_TokenId = 22;

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
            StorageItem storage = snapshot.Storages.TryGet(CreateOwnershipKey(account, innerKey));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<UState>().Balance;
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        public StackItem TokensOf(ApplicationEngine engine, Array args)
        {
            UInt160 owner = new UInt160(args[0].GetSpan());
            return new InteropInterface(TokensOf(engine.Snapshot, owner));
        }

        public virtual IEnumerator<TState> TokensOf(StoreView snapshot, UInt160 owner)
        {
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Ownership, owner).ToArray()).Select(p =>
            {
                UInt160 innerKey = new UInt160(p.Key.Key.Skip(1 + UInt160.Length).Take(UInt160.Length).ToArray());
                return snapshot.Storages.TryGet(CreateTokenKey(innerKey)).GetInteroperable<TState>();
            }).GetEnumerator();
        }

        [ContractMethod(0_01000000, ContractParameterType.InteropInterface, CallFlags.None, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "tokenId" })]
        public StackItem OwnerOf(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return new InteropInterface(OwnerOf(engine.Snapshot, tokenId));
        }

        public virtual IEnumerator<UInt160> OwnerOf(StoreView snapshot, byte[] tokenId)
        {
            UInt160 innerKey = GetInnerKey(tokenId);
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Ownership, innerKey).ToArray()).Select(p =>
            {
                return new UInt160(p.Key.Key.Skip(1 + UInt160.Length).Take(UInt160.Length).ToArray());
            }).GetEnumerator();
        }

        [ContractMethod(0_08000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.ByteArray }, ParameterNames = new[] { "from", "to", "amount", "tokenId" })]
        public virtual StackItem Transfer(ApplicationEngine engine, Array args)
        {
            UInt160 from = new UInt160(args[0].GetSpan());
            UInt160 to = new UInt160(args[1].GetSpan());
            BigInteger amount = args[2].GetBigInteger();
            byte[] tokenId = args[3].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return Transfer(engine, from, to, amount, tokenId);
        }

        public virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenId)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!InteropService.Runtime.CheckWitnessInternal(engine, from)) return false;

            var storages = engine.Snapshot.Storages;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;
            if (!amount.IsZero && !from.Equals(to))
            {
                UInt160 innerKey = GetInnerKey(tokenId);
                StorageKey fromKey = CreateOwnershipKey(from, innerKey);
                UState fromBalance = storages.GetAndChange(fromKey)?.GetInteroperable<UState>();
                if (fromBalance is null) return false;
                if (fromBalance.Balance < amount) return false;
                if (fromBalance.Balance == amount)
                {
                    storages.Delete(fromKey);
                    storages.Delete(CreateOwnershipKey(innerKey, from));
                }
                else
                {
                    fromBalance.Balance -= amount;
                }
                StorageKey toKey = CreateOwnershipKey(to, innerKey);
                UState toBalance = storages.GetAndChange(toKey, () => new StorageItem(new UState())).GetInteroperable<UState>();
                toBalance.Balance += amount;
                storages.GetAndChange(CreateOwnershipKey(innerKey, to), () => new StorageItem(new byte[0]));
            }
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount, tokenId }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "tokenId" })]
        protected StackItem Properties(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            if (tokenId.Length > MaxTokenIdLength) return false;
            return Properties(engine.Snapshot, tokenId).ToString();
        }

        public abstract JObject Properties(StoreView snapshot, byte[] tokenid);

        internal protected virtual void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId)
        {
            if (tokenId.Length > MaxTokenIdLength) throw new InvalidOperationException("The length of tokenId exceeds the maximum limit");

            var storages = engine.Snapshot.Storages;
            UInt160 innerKey = GetInnerKey(tokenId);
            StorageKey tokenKey = CreateTokenKey(innerKey);
            if (storages.TryGet(tokenKey) != null) throw new InvalidOperationException("Token already exist");

            storages.Add(tokenKey, new StorageItem(new TState() { TokenId = tokenId }));
            IncreaseTotalSupply(engine.Snapshot);

            StorageKey owner2tokenKey = CreateOwnershipKey(account, innerKey);
            StorageKey token2ownerKey = CreateOwnershipKey(innerKey, account);
            storages.Add(owner2tokenKey, new StorageItem(new UState() { Balance = Factor }));
            storages.Add(token2ownerKey, new StorageItem(new byte[0]));

            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), Factor, tokenId }));
        }

        internal protected virtual void Burn(ApplicationEngine engine, byte[] tokenId)
        {
            var storages = engine.Snapshot.Storages;
            UInt160 innerKey = GetInnerKey(tokenId);
            StorageKey tokenKey = CreateTokenKey(innerKey);
            if (!storages.Delete(tokenKey)) throw new InvalidOperationException("Token doesn't exist");

            IEnumerator<UInt160> enumerator = OwnerOf(engine.Snapshot, tokenId);
            while (!enumerator.MoveNext())
            {
                UInt160 account = enumerator.Current;
                storages.Delete(CreateOwnershipKey(innerKey, account));
                storages.Delete(CreateOwnershipKey(account, innerKey));
            }
            DecreaseTotalSupply(engine.Snapshot);
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", StackItem.Null, StackItem.Null, Factor, tokenId }));
        }

        public virtual UInt160 GetInnerKey(byte[] tokenId)
        {
            return new UInt160(Crypto.Hash160(tokenId));
        }

        private StorageKey CreateOwnershipKey(UInt160 first, UInt160 second)
        {
            byte[] byteSource = new byte[first.Size + second.Size];
            System.Array.Copy(first.ToArray(), 0, byteSource, 0, first.Size);
            System.Array.Copy(second.ToArray(), 0, byteSource, first.Size, second.Size);
            return CreateStorageKey(Prefix_Ownership, byteSource);
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
