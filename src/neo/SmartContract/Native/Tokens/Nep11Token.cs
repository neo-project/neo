#pragma warning disable IDE0060

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
    public abstract class Nep11Token<TState> : NativeContract
        where TState : Nep11TokenState, new()
    {
        public override string[] SupportedStandards { get; } = { "NEP-11", "NEP-10" };
        public abstract string Name { get; }
        public abstract string Symbol { get; }
        public abstract byte Decimals { get; }
        public BigInteger Factor { get; }

        protected const byte Prefix_TotalSupply = 11;
        protected const byte Prefix_OwnershipMapping = 25;
        protected const byte Prefix_tokenid = 26;

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
            byte[] tokenid = args[1].GetSpan().ToArray();
            return BalanceOf(engine.Snapshot, new UInt160(args[0].GetSpan()), tokenid);
        }

        public virtual BigInteger BalanceOf(StoreView snapshot, UInt160 account, byte[] tokenid)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_tokenid, tokenid));
            if (storage is null) return BigInteger.Zero;
            return storage.GetInteroperable<TState>().owners[account];
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        private StackItem TokensOf(ApplicationEngine engine, Array args)
        {
            UInt160 owner = args[0].GetSpan().AsSerializable<UInt160>();
            return new InteropInterface(TokensOf(engine.Snapshot, owner));
        }

        public virtual IEnumerator TokensOf(StoreView snapshot, UInt160 owner)
        {
            UInt256[] domains = snapshot.Storages[CreateStorageKey(Prefix_OwnershipMapping, owner)].Value.AsSerializableArray<UInt256>();
            return domains.GetEnumerator();
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.None, ParameterTypes = new[] { ContractParameterType.Hash160 }, ParameterNames = new[] { "owner" })]
        private StackItem OwnerOf(ApplicationEngine engine, Array args)
        {
            byte[] tokenid = args[0].GetSpan().ToArray();
            return new InteropInterface(OwnerOf(engine.Snapshot, tokenid));
        }
        public virtual IEnumerator OwnerOf(StoreView snapshot, byte[] tokenid)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_tokenid, tokenid)].Value.AsSerializable<TState>().owners.Keys.GetEnumerator();
        }


        [ContractMethod(0_08000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "from", "to", "amount" })]
        protected StackItem Transfer(ApplicationEngine engine, Array args)
        {
            if (args.Count != 4 && args.Count != 2) return false;
            UInt160 from = null;
            UInt160 to = null;
            BigInteger amount = -1;
            byte[] tokenid = null;
            if (args.Count == 2)
            {
                from = engine.CallingScriptHash;
                to = new UInt160(args[0].GetSpan());
                tokenid = args[1].GetSpan().ToArray();
            }
            else
            {
                from = new UInt160(args[0].GetSpan());
                to = new UInt160(args[1].GetSpan());
                amount = args[2].GetBigInteger();
                tokenid = args[3].GetSpan().ToArray();
            }
            return Transfer(engine, from, to, amount, tokenid);
        }

        protected virtual bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenid)
        {
            if (amount.Sign < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!from.Equals(engine.CallingScriptHash) && !InteropService.Runtime.CheckWitnessInternal(engine, from))
                return false;
            ContractState contract_to = engine.Snapshot.Contracts.TryGet(to);
            if (contract_to?.Payable == false) return false;

            StorageKey key_token = CreateStorageKey(Prefix_tokenid, tokenid);
            StorageItem storage_token = engine.Snapshot.Storages.GetAndChange(key_token);

            if (!amount.IsZero)
            {
                if (storage_token is null) return false;
                TState state_token = storage_token.GetInteroperable<TState>();
                if (state_token.owners[from] < amount) return false;
                if (!from.Equals(to))
                {
                    StorageKey key_from = CreateStorageKey(Prefix_OwnershipMapping, from);
                    StorageItem storage_from = engine.Snapshot.Storages.GetAndChange(key_from);

                    SortedSet<UInt256> domains_from = null;
                    if (storage_from is null) { domains_from = new SortedSet<UInt256>(); }
                    else
                    {
                        domains_from = new SortedSet<UInt256>(storage_from.Value.AsSerializableArray<UInt256>());
                    }


                    StorageKey key_to = CreateStorageKey(Prefix_OwnershipMapping, to);
                    StorageItem storage_to = engine.Snapshot.Storages.GetAndChange(key_to);
                    SortedSet<UInt256> domains_to = null;
                    if (storage_to is null) { domains_to = new SortedSet<UInt256>(); }
                    else
                    {
                        domains_to = new SortedSet<UInt256>(storage_to.Value.AsSerializableArray<UInt256>());
                    }

                    if (state_token.owners[from] == amount)
                    {
                        engine.Snapshot.Storages.Delete(key_from);
                        domains_from.Remove(tokenid.AsSerializable<UInt256>());
                    }
                    else
                    {
                        state_token.owners[from] -= amount;
                    }

                    if (!state_token.owners.ContainsKey(to))
                        state_token.owners.Add(to, amount);
                    else
                        state_token.owners[to] += amount;
                    domains_to.Add(tokenid.AsSerializable<UInt256>());
                }
            }
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount, tokenid }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Hash160, ContractParameterType.Integer }, ParameterNames = new[] { "from", "to", "amount" })]
        protected StackItem Properties(ApplicationEngine engine, Array args)
        {
            byte[] tokenid = args[0].GetSpan().ToArray();
            return Properties(engine.Snapshot, tokenid);
        }

        public virtual String Properties(StoreView snapshot, byte[] tokenid)
        {
            return "";
        }
    }
}
