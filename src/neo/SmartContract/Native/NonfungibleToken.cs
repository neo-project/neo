using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// The base class of all native tokens that are compatible with NEP-11.
    /// </summary>
    /// <typeparam name="TokenState">The type of the token state.</typeparam>
    public abstract class NonfungibleToken<TokenState> : NativeContract
        where TokenState : NFTState, new()
    {
        /// <summary>
        /// The symbol of the token.
        /// </summary>
        [ContractMethod]
        public abstract string Symbol { get; }

        /// <summary>
        /// The number of decimal places of the token. It always return 0 in native NFT.
        /// </summary>
        [ContractMethod]
        public byte Decimals => 0;

        private const byte Prefix_TotalSupply = 11;
        private const byte Prefix_Account = 7;

        /// <summary>
        /// The prefix for storing token states.
        /// </summary>
        protected const byte Prefix_Token = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonfungibleToken{TokenState}"/> class.
        /// </summary>
        protected NonfungibleToken()
        {
            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractEventDescriptor()
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
                    }
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        /// <summary>
        /// Gets the storage key of the specified token id.
        /// </summary>
        /// <param name="tokenId">The id of the token.</param>
        /// <returns>The storage key.</returns>
        protected virtual byte[] GetKey(byte[] tokenId) => tokenId;

        internal override ContractTask Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Add(CreateStorageKey(Prefix_TotalSupply), new StorageItem(BigInteger.Zero));
            return ContractTask.CompletedTask;
        }

        private protected ContractTask Mint(ApplicationEngine engine, TokenState token)
        {
            engine.Snapshot.Add(CreateStorageKey(Prefix_Token).Add(GetKey(token.Id)), new StorageItem(token));
            NFTAccountState account = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Account).Add(token.Owner), () => new StorageItem(new NFTAccountState())).GetInteroperable<NFTAccountState>();
            account.Add(token.Id);
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_TotalSupply)).Add(1);
            return PostTransfer(engine, null, token.Owner, token.Id, StackItem.Null);
        }

        private protected ContractTask Burn(ApplicationEngine engine, byte[] tokenId)
        {
            return Burn(engine, CreateStorageKey(Prefix_Token).Add(GetKey(tokenId)));
        }

        private protected ContractTask Burn(ApplicationEngine engine, StorageKey key)
        {
            TokenState token = engine.Snapshot.TryGet(key)?.GetInteroperable<TokenState>();
            if (token is null) throw new InvalidOperationException();
            engine.Snapshot.Delete(key);
            StorageKey key_account = CreateStorageKey(Prefix_Account).Add(token.Owner);
            NFTAccountState account = engine.Snapshot.GetAndChange(key_account).GetInteroperable<NFTAccountState>();
            account.Remove(token.Id);
            if (account.Balance.IsZero)
                engine.Snapshot.Delete(key_account);
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_TotalSupply)).Add(-1);
            return PostTransfer(engine, token.Owner, null, token.Id, StackItem.Null);
        }

        /// <summary>
        /// Gets the total supply of the token.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The total supply of the token.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public BigInteger TotalSupply(DataCache snapshot)
        {
            return snapshot[CreateStorageKey(Prefix_TotalSupply)];
        }

        /// <summary>
        /// Gets the owner of the token.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="tokenId">The id of the token.</param>
        /// <returns>The owner of the token.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public UInt160 OwnerOf(DataCache snapshot, byte[] tokenId)
        {
            return snapshot[CreateStorageKey(Prefix_Token).Add(GetKey(tokenId))].GetInteroperable<TokenState>().Owner;
        }

        /// <summary>
        /// Gets the properties of the token.
        /// </summary>
        /// <param name="engine">The <see cref="ApplicationEngine"/> that is executing the contract.</param>
        /// <param name="tokenId">The id of the token.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public Map Properties(ApplicationEngine engine, byte[] tokenId)
        {
            return engine.Snapshot[CreateStorageKey(Prefix_Token).Add(GetKey(tokenId))].GetInteroperable<TokenState>().ToMap(engine.ReferenceCounter);
        }

        /// <summary>
        /// Gets the balance of the specified account.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="owner">The owner of the account.</param>
        /// <returns>The balance of the account. Or 0 if the account doesn't exist.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public BigInteger BalanceOf(DataCache snapshot, UInt160 owner)
        {
            if (owner is null) throw new ArgumentNullException(nameof(owner));
            return snapshot.TryGet(CreateStorageKey(Prefix_Account).Add(owner))?.GetInteroperable<NFTAccountState>().Balance ?? BigInteger.Zero;
        }

        /// <summary>
        /// Get all the tokens that have been issued in the contract.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>All the tokens that have been issued.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        protected IIterator Tokens(DataCache snapshot)
        {
            var results = snapshot.Find(CreateStorageKey(Prefix_Token).ToArray()).GetEnumerator();
            return new StorageIterator(results, 1, FindOptions.ValuesOnly | FindOptions.DeserializeValues | FindOptions.PickField1, null);
        }

        /// <summary>
        /// Gets all the tokens owned by the specified account.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="owner">The owner of the account.</param>
        /// <returns>All the tokens owned by the specified account.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        protected IIterator TokensOf(DataCache snapshot, UInt160 owner)
        {
            NFTAccountState account = snapshot.TryGet(CreateStorageKey(Prefix_Account).Add(owner))?.GetInteroperable<NFTAccountState>();
            IReadOnlyList<byte[]> tokens = account?.Tokens ?? (IReadOnlyList<byte[]>)System.Array.Empty<byte[]>();
            return new ArrayWrapper(tokens.Select(p => (StackItem)p).ToArray());
        }

        [ContractMethod(CpuFee = 1 << 17, StorageFee = 50, RequiredCallFlags = CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify)]
        private protected async ContractTask<bool> Transfer(ApplicationEngine engine, UInt160 to, byte[] tokenId, StackItem data)
        {
            if (to is null) throw new ArgumentNullException(nameof(to));
            StorageKey key_token = CreateStorageKey(Prefix_Token).Add(GetKey(tokenId));
            TokenState token = engine.Snapshot.TryGet(key_token)?.GetInteroperable<TokenState>();
            UInt160 from = token.Owner;
            if (!from.Equals(engine.CallingScriptHash) && !engine.CheckWitnessInternal(from))
                return false;
            if (!from.Equals(to))
            {
                token = engine.Snapshot.GetAndChange(key_token).GetInteroperable<TokenState>();
                StorageKey key_from = CreateStorageKey(Prefix_Account).Add(from);
                NFTAccountState account = engine.Snapshot.GetAndChange(key_from).GetInteroperable<NFTAccountState>();
                account.Remove(tokenId);
                if (account.Balance.IsZero)
                    engine.Snapshot.Delete(key_from);
                token.Owner = to;
                StorageKey key_to = CreateStorageKey(Prefix_Account).Add(to);
                account = engine.Snapshot.GetAndChange(key_to, () => new StorageItem(new NFTAccountState())).GetInteroperable<NFTAccountState>();
                account.Add(tokenId);
                OnTransferred(engine, from, token);
            }
            await PostTransfer(engine, from, to, tokenId, data);
            return true;
        }

        /// <summary>
        /// Called when a token is transferred.
        /// </summary>
        /// <param name="engine">The <see cref="ApplicationEngine"/> that is executing the contract.</param>
        /// <param name="from">The account where the token is transferred from.</param>
        /// <param name="token">The token that is transferred.</param>
        protected virtual void OnTransferred(ApplicationEngine engine, UInt160 from, TokenState token)
        {
        }

        private async ContractTask PostTransfer(ApplicationEngine engine, UInt160 from, UInt160 to, byte[] tokenId, StackItem data)
        {
            engine.SendNotification(Hash, "Transfer",
                new Array { from?.ToArray() ?? StackItem.Null, to?.ToArray() ?? StackItem.Null, 1, tokenId });

            if (to is not null && ContractManagement.GetContract(engine.Snapshot, to) is not null)
                await engine.CallFromNativeContract(Hash, to, "onNEP11Payment", from?.ToArray() ?? StackItem.Null, 1, tokenId, data);
        }

        class NFTAccountState : AccountState
        {
            public readonly List<byte[]> Tokens = new();

            public void Add(byte[] tokenId)
            {
                Balance++;
                int index = ~Tokens.BinarySearch(tokenId, ByteArrayComparer.Default);
                Tokens.Insert(index, tokenId);
            }

            public void Remove(byte[] tokenId)
            {
                Balance--;
                int index = Tokens.BinarySearch(tokenId, ByteArrayComparer.Default);
                Tokens.RemoveAt(index);
            }

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Array array = (Array)((Struct)stackItem)[1];
                Tokens.AddRange(array.Select(p => p.GetSpan().ToArray()));
            }

            public override StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = (Struct)base.ToStackItem(referenceCounter);
                @struct.Add(new Array(referenceCounter, Tokens.Select(p => (StackItem)p)));
                return @struct;
            }
        }
    }
}
