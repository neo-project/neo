#pragma warning disable IDE0060

using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
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
    public abstract class Nep11Token<TState,UState> : NativeContract
        where TState : Nep11TokenState, new()
        where UState : Nep11AccountState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-11", "NEP-10" };
        public abstract string Name { get; }
        public abstract string Symbol { get; }
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_TotalSupply = 11;
        protected const byte Prefix_Owner2TokenMapping = 25;
        protected const byte Prefix_Token2OwnerMapping = 26;
        protected const byte Prefix_TokenID = 27;

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

        [ContractMethod(0_01000000, ContractParameterType.Integer, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "account" })]
        protected StackItem BalanceOf(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            UInt256 tokenid = Transform(args[1].GetSpan().ToArray());
            return BalanceOf(engine.Snapshot, account, tokenid);
        }

        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account, UInt256 tokenid)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateOwner2TokenKey(account,tokenid));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<UState>().Balance;
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        private StackItem TokensOf(ApplicationEngine engine, Array args)
        {
            UInt160 owner = new UInt160(args[0].GetSpan());
            return new InteropInterface(TokensOf(engine.Snapshot, owner));
        }

        public virtual IEnumerator TokensOf(StoreView snapshot, UInt160 owner)
        {
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Owner2TokenMapping, owner).ToArray()).Select(p=> {
                byte[] tokenId = p.Key.Key.Skip(1 + UInt160.Length).Take(UInt256.Length).ToArray();
                return snapshot.Storages.TryGet(CreateStorageKey(Prefix_TokenID, tokenId)).Value.AsSerializable<TState>();
            }).GetEnumerator();
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.None, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        private StackItem OwnerOf(ApplicationEngine engine, Array args)
        {
            UInt256 tokenid = Transform(args[0].GetSpan().ToArray());
            return new InteropInterface(OwnerOf(engine.Snapshot, tokenid));
        }
        public virtual IEnumerator OwnerOf(StoreView snapshot, UInt256 tokenid)
        {
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Token2OwnerMapping,tokenid).ToArray()).Select(p=> {
                return new UInt160(p.Key.Key.Skip(1 + UInt256.Length).Take(UInt160.Length).ToArray());
            }).GetEnumerator();
        }


        [ContractMethod(0_08000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "from", "to", "amount" })]
        public virtual StackItem Transfer(ApplicationEngine engine, Array args)
        {
            if (args.Count != 4 && args.Count != 2) return false;
            UInt160 from = null;
            UInt160 to = null;
            BigInteger amount = -1;
            UInt256 tokenid = null;
            if (args.Count == 2)
            {
                from = engine.CallingScriptHash;
                to = new UInt160(args[0].GetSpan());
                tokenid = Transform(args[1].GetSpan().ToArray());
            }
            else
            {
                from = new UInt160(args[0].GetSpan());
                to = new UInt160(args[1].GetSpan());
                amount = args[2].GetBigInteger();
                tokenid = Transform(args[3].GetSpan().ToArray());
            }
            return Transfer(engine, from, to, amount, tokenid);
        }

        protected virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, UInt256 tokenid)
        {
            //check amount range.
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            //check witness
            if (!from.Equals(engine.CallingScriptHash) && !InteropService.Runtime.CheckWitnessInternal(engine, from))
                return false;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;
            if (!amount.IsZero)
            {
                //Is exist token
                StorageKey key_token = CreateTokenKey(tokenid);
                StorageItem storage_token = engine.Snapshot.Storages.GetAndChange(key_token);
                if (storage_token is null) return false;
                if (!from.Equals(to))
                {
                    //Check from account
                    StorageKey key_from = CreateOwner2TokenKey(from, tokenid);
                    StorageItem storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                    if (storage_from is null) return false;
                    UState accountstate_from = storage_from.Value.AsSerializable<UState>();
                    if (accountstate_from.Balance < amount) return false;
                    //change from account state
                    if (accountstate_from.Balance == amount)
                    {
                        engine.Snapshot.Storages.Delete(key_from);
                        engine.Snapshot.Storages.Delete(CreateToken2OwnerKey(tokenid, from));
                    }
                    else
                    {
                        accountstate_from.Balance -= amount;
                        storage_from.Value = accountstate_from.ToArray();
                        key_from = CreateToken2OwnerKey(tokenid, from);
                        storage_from = engine.Snapshot.Storages.GetAndChange(key_from);
                        storage_from.Value = accountstate_from.ToArray();
                    }
                    //change to account state
                    StorageKey key_to = CreateOwner2TokenKey(to, tokenid);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem()
                    {
                        Value = new UState() { Balance = 0 }.ToArray()
                    });
                    UState accountstate_to = storage_to.Value.AsSerializable<UState>();
                    accountstate_to.Balance += amount;
                    storage_to.Value = accountstate_to.ToArray();
                    key_to = CreateToken2OwnerKey(tokenid, to);
                    storage_to = engine.Snapshot.Storages.GetAndChange(key_to, () => new StorageItem()
                    {
                        Value = new UState() { Balance = 0 }.ToArray()
                    });
                    accountstate_to = storage_to.Value.AsSerializable<UState>();
                    accountstate_to.Balance += amount;
                    storage_to.Value = accountstate_to.ToArray();
                }
            }
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount, tokenid.ToArray() }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "from", "to", "amount" })]
        protected StackItem Properties(ApplicationEngine engine, Array args)
        {
            UInt256 tokenid = Transform(args[0].GetSpan().ToArray());
            return Properties(engine.Snapshot, tokenid);
        }

        public virtual String Properties(StoreView snapshot, UInt256 tokenid)
        {
            return "{}";
        }

        public virtual UInt256 Transform(byte[] parameter) {
            return new UInt256(parameter);
        }

        protected StorageKey CreateOwner2TokenKey(UInt160 owner,UInt256 tokenid)
        {
            byte[] byteSource = new byte[owner.Size+tokenid.Size];
            System.Array.Copy(owner.ToArray(),0, byteSource,0,owner.Size);
            System.Array.Copy(tokenid.ToArray(),0, byteSource, owner.Size, tokenid.Size);
            return CreateStorageKey(Prefix_Owner2TokenMapping, byteSource);
        }

        protected StorageKey CreateToken2OwnerKey(UInt256 tokenid,UInt160 owner)
        {
            byte[] byteSource = new byte[owner.Size + tokenid.Size];
            System.Array.Copy(tokenid.ToArray(), 0, byteSource, 0, tokenid.Size);
            System.Array.Copy(owner.ToArray(), 0, byteSource, tokenid.Size, owner.Size);
            return CreateStorageKey(Prefix_Token2OwnerMapping, byteSource);
        }

        protected StorageKey CreateTokenKey(UInt256 tokenid)
        {
            return CreateStorageKey(Prefix_TokenID, tokenid.ToArray());
        }

        public void Accumulator(ApplicationEngine engine)
        {
            StorageItem storage_totalSupply = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
            storage_totalSupply.Value = (new BigInteger(storage_totalSupply.Value) + BigInteger.One).ToByteArray();
        }
    }
}
