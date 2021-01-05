#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Neo.SmartContract.Native
{
    public sealed class NameService : NonfungibleToken<NameService.NameState>
    {
        public override int Id => -6;
        public override string Symbol => "NNS";

        private const byte Prefix_Roots = 10;
        private const byte Prefix_DomainPrice = 22;
        private const byte Prefix_Expiration = 20;

        private const uint OneYear = 365 * 24 * 3600;
        private static readonly Regex rootRegex = new Regex("^[a-z][a-z0-9]{0,15}$");
        private static readonly Regex nameRegex = new Regex("^(?=.{3,255}$)([a-z0-9]{1,62}\\.)+[a-z][a-z0-9]{0,15}$");

        internal NameService()
        {
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_DomainPrice), new StorageItem(10_00000000));
        }

        internal override void OnPersist(ApplicationEngine engine)
        {
            uint now = (uint)(engine.Snapshot.PersistingBlock.Timestamp / 1000) + 1;
            byte[] start = CreateStorageKey(Prefix_Expiration).AddBigEndian(0).ToArray();
            byte[] end = CreateStorageKey(Prefix_Expiration).AddBigEndian(now).ToArray();
            foreach (var (key, _) in engine.Snapshot.Storages.FindRange(start, end))
            {
                engine.Snapshot.Storages.Delete(key);
                Burn(engine, CreateStorageKey(Prefix_Token).Add(key.Key.AsSpan(5)));
            }
        }

        protected override byte[] GetKey(byte[] tokenId)
        {
            return Crypto.Hash160(tokenId);
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void AddRoot(ApplicationEngine engine, string root)
        {
            if (!rootRegex.IsMatch(root)) throw new ArgumentException(null, nameof(root));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StringList roots = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Roots), () => new StorageItem(new StringList())).GetInteroperable<StringList>();
            int index = roots.BinarySearch(root);
            if (index >= 0) throw new InvalidOperationException("The name already exists.");
            roots.Insert(~index, root);
        }

        public IEnumerable<string> GetRoots(StoreView snapshot)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Roots))?.GetInteroperable<StringList>() ?? Enumerable.Empty<string>();
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetPrice(ApplicationEngine engine, long price)
        {
            if (price <= 0 || price > 10000_00000000) throw new ArgumentOutOfRangeException(nameof(price));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_DomainPrice)).Set(price);
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public long GetPrice(StoreView snapshot)
        {
            return (long)(BigInteger)snapshot.Storages[CreateStorageKey(Prefix_DomainPrice)];
        }

        [ContractMethod(0_01000000, CallFlags.WriteStates)]
        private bool Register(ApplicationEngine engine, string name, UInt160 owner)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string[] names = name.Split('.');
            if (names.Length != 2) throw new ArgumentException(null, nameof(name));
            if (!engine.CheckWitnessInternal(owner)) throw new InvalidOperationException();
            byte[] hash = GetKey(Utility.StrictUTF8.GetBytes(name));
            if (engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Token).Add(hash)) is not null) return false;
            StringList roots = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Roots))?.GetInteroperable<StringList>();
            if (roots is null || roots.BinarySearch(names[1]) < 0) throw new InvalidOperationException();
            engine.AddGas(GetPrice(engine.Snapshot));
            NameState state = new NameState
            {
                Owner = owner,
                Name = name,
                Description = "",
                Expiration = (uint)(engine.Snapshot.PersistingBlock.Timestamp / 1000) + OneYear
            };
            Mint(engine, state);
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Expiration).AddBigEndian(state.Expiration).Add(hash), new StorageItem(new byte[] { 0 }));
            return true;
        }

        [ContractMethod(0, CallFlags.WriteStates)]
        private uint Renew(ApplicationEngine engine, string name)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string[] names = name.Split('.');
            if (names.Length != 2) throw new ArgumentException(null, nameof(name));
            engine.AddGas(GetPrice(engine.Snapshot));
            byte[] hash = GetKey(Utility.StrictUTF8.GetBytes(name));
            NameState state = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Token).Add(hash)).GetInteroperable<NameState>();
            engine.Snapshot.Storages.Delete(CreateStorageKey(Prefix_Expiration).AddBigEndian(state.Expiration).Add(hash));
            state.Expiration += OneYear;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Expiration).AddBigEndian(state.Expiration).Add(hash), new StorageItem(new byte[] { 0 }));
            return state.Expiration;
        }

        public class NameState : NFTState
        {
            public uint Expiration;

            public override byte[] Id => Utility.StrictUTF8.GetBytes(Name);

            public override JObject ToJson()
            {
                JObject json = base.ToJson();
                json["expiration"] = Expiration;
                return json;
            }

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Struct @struct = (Struct)stackItem;
                Expiration = (uint)@struct[3].GetInteger();
            }

            public override StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = (Struct)base.ToStackItem(referenceCounter);
                @struct.Add(Expiration);
                return @struct;
            }
        }

        private class StringList : List<string>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (VM.Types.Array)stackItem)
                    Add(item.GetString());
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new VM.Types.Array(referenceCounter, this.Select(p => (ByteString)p));
            }
        }
    }
}
