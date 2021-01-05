#pragma warning disable IDE0051

using Neo.Cryptography;
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

        private const byte Prefix_Roots = 5;
        private const byte Prefix_DomainPrice = 11;

        private static readonly Regex rootRegex = new Regex("^[a-z][a-z0-9]{0,15}$");

        internal NameService()
        {
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_DomainPrice), new StorageItem(10_00000000));
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

        public class NameState : NFTState
        {
            public override byte[] Id => Utility.StrictUTF8.GetBytes(Name);
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
