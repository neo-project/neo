#pragma warning disable IDE0060

using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Tokens
{
    public abstract class Nep11Token<TState, UState> : NativeContract
        where TState : Nep11TokenState, new()
        where UState : Nep11AccountState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-11", "NEP-10" };
        public abstract string Name { get; }
        public abstract string Symbol { get; }
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_TotalSupply = 20;
        protected const byte Prefix_Owner2TokenMapping = 21;
        protected const byte Prefix_Token2OwnerMapping = 22;
        protected const byte Prefix_TokenId = 23;

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
                            Name = "tokenid",
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
            return BalanceOf(engine.Snapshot, account, tokenId);
        }

        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account, byte[] tokenId)
        {
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageItem storage = snapshot.Storages.TryGet(CreateOwner2TokenKey(account, innerKey));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<UState>().Balance;
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        public StackItem TokensOf(ApplicationEngine engine, Array args)
        {
            UInt160 owner = new UInt160(args[0].GetSpan());
            return new InteropInterface(TokensOf(engine.Snapshot, owner));
        }

        public virtual IEnumerator TokensOf(StoreView snapshot, UInt160 owner)
        {
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Owner2TokenMapping, owner).ToArray()).Select(p =>
            {
                UInt256 innerKey = new UInt256(p.Key.Key.Skip(1 + UInt160.Length).Take(UInt256.Length).ToArray());
                return snapshot.Storages.TryGet(CreateTokenKey(innerKey)).GetInteroperable<TState>();
            }).GetEnumerator();
        }

        [ContractMethod(0_01000000, ContractParameterType.InteropInterface, CallFlags.None, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "tokenId" })]
        public StackItem OwnerOf(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            return new InteropInterface(OwnerOf(engine.Snapshot, tokenId));
        }

        public virtual IEnumerator OwnerOf(StoreView snapshot, byte[] tokenId)
        {
            UInt256 innerKey = GetInnerKey(tokenId);
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Token2OwnerMapping, innerKey).ToArray()).Select(p =>
            {
                return new UInt160(p.Key.Key.Skip(1 + UInt256.Length).Take(UInt160.Length).ToArray());
            }).GetEnumerator();
        }

        [ContractMethod(0_08000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.ByteArray }, ParameterNames = new[] { "from", "to", "amount", "tokenId" })]
        public virtual StackItem Transfer(ApplicationEngine engine, Array args)
        {
            UInt160 from = new UInt160(args[0].GetSpan());
            UInt160 to = new UInt160(args[1].GetSpan());
            BigInteger amount = args[2].GetBigInteger();
            byte[] tokenId = args[3].GetSpan().ToArray();
            return Transfer(engine, from, to, amount, tokenId);
        }

        public virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenId)
        {
            UInt256 innerKey = GetInnerKey(tokenId);
            //check amount range.
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            //check witness
            if (!InteropService.Runtime.CheckWitnessInternal(engine, from))
                return false;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;
            if (!amount.IsZero)
            {
                //Is exist token
                StorageKey key_token = CreateTokenKey(innerKey);
                StorageItem storage_token = engine.Snapshot.Storages.TryGet(key_token);
                if (storage_token is null) return false;
                if (!from.Equals(to))
                {
                    //Check from account
                    StorageKey key_from = CreateOwner2TokenKey(from, innerKey);
                    StorageItem storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                    if (storage_from is null) return false;
                    UState accountstate_from = storage_from.GetInteroperable<UState>();
                    if (accountstate_from.Balance < amount) return false;
                    //change from account state
                    if (accountstate_from.Balance == amount)
                    {
                        engine.Snapshot.Storages.Delete(key_from);
                        engine.Snapshot.Storages.Delete(CreateToken2OwnerKey(innerKey, from));
                    }
                    else
                    {
                        accountstate_from.Balance -= amount;
                        key_from = CreateToken2OwnerKey(innerKey, from);
                        storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                        accountstate_from = storage_from.GetInteroperable<UState>();
                        accountstate_from.Balance -= amount;
                    }
                    //change to account state
                    StorageKey key_to = CreateOwner2TokenKey(to, innerKey);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem(new UState() { Balance = 0 }));
                    UState accountstate_to = storage_to.GetInteroperable<UState>();
                    accountstate_to.Balance += amount;
                    key_to = CreateToken2OwnerKey(innerKey, to);
                    storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem(new UState() { Balance = 0 }));
                    accountstate_to = storage_to.GetInteroperable<UState>();
                    accountstate_to.Balance += amount;
                }
            }
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount, tokenId }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "tokenId" })]
        protected StackItem Properties(ApplicationEngine engine, Array args)
        {
            byte[] tokenid = args[0].GetSpan().ToArray();
            return Properties(engine.Snapshot, tokenid);
        }

        public virtual String Properties(StoreView snapshot, byte[] tokenid)
        {
            return "{}";
        }

        internal protected virtual void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId)
        {
            UInt256 innerKey = GetInnerKey(tokenId);
            TState token_state = new TState() { Name = System.Text.Encoding.UTF8.GetString(tokenId) };
            StorageKey token_key = CreateTokenKey(innerKey);
            StorageItem token_storage = engine.Snapshot.Storages.TryGet(token_key);
            if (token_storage != null) throw new InvalidOperationException("Token is exist");

            StorageKey owner2token_key = CreateOwner2TokenKey(account, innerKey);
            StorageItem owner2token_storage = engine.Snapshot.Storages.TryGet(owner2token_key);
            if (owner2token_storage != null) throw new InvalidOperationException("Token is exist");
            engine.Snapshot.Storages.Add(owner2token_key, new StorageItem(new UState() { Balance = Factor }));

            StorageKey token2owner_key = CreateToken2OwnerKey(innerKey, account);
            engine.Snapshot.Storages.Add(token2owner_key, new StorageItem(new UState() { Balance = Factor }));

            engine.Snapshot.Storages.Add(token_key, new StorageItem(token_state));
            IncreaseTotalSupply(engine);
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", StackItem.Null, account.ToArray(), Factor, tokenId }));
        }

        internal protected virtual void Burn(ApplicationEngine engine, UInt160 account, BigInteger amount, byte[] tokenId)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount.IsZero) return;
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageKey token_key = CreateTokenKey(innerKey);
            StorageItem token_storage = engine.Snapshot.Storages.TryGet(token_key);
            if (token_storage is null) throw new InvalidOperationException("Token is not exist");

            StorageKey owner2token_key = CreateOwner2TokenKey(account, innerKey);
            StorageItem owner2token_storage = engine.Snapshot.Storages.GetAndChange(owner2token_key);
            if (owner2token_storage is null) throw new InvalidOperationException("Account is not exist");
            StorageKey token2owner_key = CreateToken2OwnerKey(innerKey, account);
            StorageItem token2owner_storage = engine.Snapshot.Storages.GetAndChange(token2owner_key);
            UState owner2token_state = owner2token_storage.GetInteroperable<UState>();
            if (owner2token_state.Balance < amount) throw new InvalidOperationException();
            if (owner2token_state.Balance == amount)
            {
                engine.Snapshot.Storages.Delete(owner2token_key);
                engine.Snapshot.Storages.Delete(token2owner_key);
                if (!OwnerOf(engine.Snapshot, tokenId).MoveNext())
                {
                    engine.Snapshot.Storages.Delete(token_key);
                    DecreaseTotalSupply(engine);
                }
            }
            else
            {
                UState token2owner_state = token2owner_storage.GetInteroperable<UState>();
                owner2token_state.Balance -= amount;
                token2owner_state.Balance -= amount;
            }
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", account.ToArray(), StackItem.Null, amount, tokenId }));
        }

        public virtual UInt256 GetInnerKey(byte[] tokenId)
        {
            return new UInt256(tokenId.Sha256());
        }

        protected StorageKey CreateOwner2TokenKey(UInt160 owner, UInt256 innerKey)
        {
            byte[] byteSource = new byte[owner.Size + innerKey.Size];
            System.Array.Copy(owner.ToArray(), 0, byteSource, 0, owner.Size);
            System.Array.Copy(innerKey.ToArray(), 0, byteSource, owner.Size, innerKey.Size);
            return CreateStorageKey(Prefix_Owner2TokenMapping, byteSource);
        }

        protected StorageKey CreateToken2OwnerKey(UInt256 innerKey, UInt160 owner)
        {
            byte[] byteSource = new byte[owner.Size + innerKey.Size];
            System.Array.Copy(innerKey.ToArray(), 0, byteSource, 0, innerKey.Size);
            System.Array.Copy(owner.ToArray(), 0, byteSource, innerKey.Size, owner.Size);
            return CreateStorageKey(Prefix_Token2OwnerMapping, byteSource);
        }

        protected StorageKey CreateTokenKey(UInt256 innerKey)
        {
            return CreateStorageKey(Prefix_TokenId, innerKey.ToArray());
        }

        public void IncreaseTotalSupply(ApplicationEngine engine)
        {
            StorageItem storage_totalSupply = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
            storage_totalSupply.Value = (new BigInteger(storage_totalSupply.Value) + BigInteger.One).ToByteArray();
        }

        public void DecreaseTotalSupply(ApplicationEngine engine)
        {
            StorageItem storage_totalSupply = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
            BigInteger totalSupply = new BigInteger(storage_totalSupply.Value);
            if (totalSupply.Equals(BigInteger.Zero)) return;
            storage_totalSupply.Value = (totalSupply - BigInteger.One).ToByteArray();
        }
    }
}
