#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.IO;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Neo.SmartContract.Native
{
    public sealed class NameService : NonfungibleToken<NameService.NameState>
    {
        public override string Symbol => "NNS";

        private const byte Prefix_Roots = 10;
        private const byte Prefix_DomainPrice = 22;
        private const byte Prefix_Expiration = 20;
        private const byte Prefix_Record = 12;

        private const uint OneYear = 365 * 24 * 3600;
        private static readonly Regex rootRegex = new Regex("^[a-z][a-z0-9]{0,15}$", RegexOptions.Singleline);
        private static readonly Regex nameRegex = new Regex("^(?=.{3,255}$)([a-z0-9]{1,62}\\.)+[a-z][a-z0-9]{0,15}$", RegexOptions.Singleline);
        private static readonly Regex ipv4Regex = new Regex("^(?=\\d+\\.\\d+\\.\\d+\\.\\d+$)(?:(?:25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9][0-9]|[0-9])\\.?){4}$", RegexOptions.Singleline);
        private static readonly Regex ipv6Regex = new Regex("(?:^)(([0-9a-f]{1,4}:){7,7}[0-9a-f]{1,4}|([0-9a-f]{1,4}:){1,7}:|([0-9a-f]{1,4}:){1,6}:[0-9a-f]{1,4}|([0-9a-f]{1,4}:){1,5}(:[0-9a-f]{1,4}){1,2}|([0-9a-f]{1,4}:){1,4}(:[0-9a-f]{1,4}){1,3}|([0-9a-f]{1,4}:){1,3}(:[0-9a-f]{1,4}){1,4}|([0-9a-f]{1,4}:){1,2}(:[0-9a-f]{1,4}){1,5}|[0-9a-f]{1,4}:((:[0-9a-f]{1,4}){1,6})|:((:[0-9a-f]{1,4}){1,7}|:))(?=$)", RegexOptions.Singleline);

        internal NameService()
        {
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            base.Initialize(engine);
            engine.Snapshot.Add(CreateStorageKey(Prefix_Roots), new StorageItem(new StringList()));
            engine.Snapshot.Add(CreateStorageKey(Prefix_DomainPrice), new StorageItem(10_00000000));
        }

        internal override void OnPersist(ApplicationEngine engine)
        {
            uint now = (uint)(engine.PersistingBlock.Timestamp / 1000) + 1;
            byte[] start = CreateStorageKey(Prefix_Expiration).AddBigEndian(0).ToArray();
            byte[] end = CreateStorageKey(Prefix_Expiration).AddBigEndian(now).ToArray();
            foreach (var (key, _) in engine.Snapshot.FindRange(start, end))
            {
                engine.Snapshot.Delete(key);
                foreach (var (key2, _) in engine.Snapshot.Find(CreateStorageKey(Prefix_Record).Add(key.Key.AsSpan(5)).ToArray()))
                    engine.Snapshot.Delete(key2);
                Burn(engine, CreateStorageKey(Prefix_Token).Add(key.Key.AsSpan(5)));
            }
        }

        protected override void OnTransferred(ApplicationEngine engine, UInt160 from, NameState token)
        {
            token.Admin = null;
        }

        protected override byte[] GetKey(byte[] tokenId)
        {
            return Crypto.Hash160(tokenId);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void AddRoot(ApplicationEngine engine, string root)
        {
            if (!rootRegex.IsMatch(root)) throw new ArgumentException(null, nameof(root));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            StringList roots = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Roots)).GetInteroperable<StringList>();
            int index = roots.BinarySearch(root);
            if (index >= 0) throw new InvalidOperationException("The name already exists.");
            roots.Insert(~index, root);
        }

        public IEnumerable<string> GetRoots(DataCache snapshot)
        {
            return snapshot[CreateStorageKey(Prefix_Roots)].GetInteroperable<StringList>();
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void SetPrice(ApplicationEngine engine, long price)
        {
            if (price <= 0 || price > 10000_00000000) throw new ArgumentOutOfRangeException(nameof(price));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_DomainPrice)).Set(price);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public long GetPrice(DataCache snapshot)
        {
            return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_DomainPrice)];
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public bool IsAvailable(DataCache snapshot, string name)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string[] names = name.Split('.');
            if (names.Length != 2) throw new ArgumentException(null, nameof(name));
            byte[] hash = GetKey(Utility.StrictUTF8.GetBytes(name));
            if (snapshot.TryGet(CreateStorageKey(Prefix_Token).Add(hash)) is not null) return false;
            StringList roots = snapshot[CreateStorageKey(Prefix_Roots)].GetInteroperable<StringList>();
            if (roots.BinarySearch(names[1]) < 0) throw new InvalidOperationException();
            return true;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private bool Register(ApplicationEngine engine, string name, UInt160 owner)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string[] names = name.Split('.');
            if (names.Length != 2) throw new ArgumentException(null, nameof(name));
            if (!engine.CheckWitnessInternal(owner)) throw new InvalidOperationException();
            byte[] hash = GetKey(Utility.StrictUTF8.GetBytes(name));
            if (engine.Snapshot.TryGet(CreateStorageKey(Prefix_Token).Add(hash)) is not null) return false;
            StringList roots = engine.Snapshot[CreateStorageKey(Prefix_Roots)].GetInteroperable<StringList>();
            if (roots.BinarySearch(names[1]) < 0) throw new InvalidOperationException();
            engine.AddGas(GetPrice(engine.Snapshot));
            NameState state = new NameState
            {
                Owner = owner,
                Name = name,
                Expiration = (uint)(engine.PersistingBlock.Timestamp / 1000) + OneYear
            };
            Mint(engine, state);
            engine.Snapshot.Add(CreateStorageKey(Prefix_Expiration).AddBigEndian(state.Expiration).Add(hash), new StorageItem(new byte[] { 0 }));
            return true;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private uint Renew(ApplicationEngine engine, string name)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string[] names = name.Split('.');
            if (names.Length != 2) throw new ArgumentException(null, nameof(name));
            engine.AddGas(GetPrice(engine.Snapshot));
            byte[] hash = GetKey(Utility.StrictUTF8.GetBytes(name));
            NameState state = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Token).Add(hash)).GetInteroperable<NameState>();
            engine.Snapshot.Delete(CreateStorageKey(Prefix_Expiration).AddBigEndian(state.Expiration).Add(hash));
            state.Expiration += OneYear;
            engine.Snapshot.Add(CreateStorageKey(Prefix_Expiration).AddBigEndian(state.Expiration).Add(hash), new StorageItem(new byte[] { 0 }));
            return state.Expiration;
        }

        [ContractMethod(CpuFee = 1 << 15, StorageFee = 20, RequiredCallFlags = CallFlags.States)]
        private void SetAdmin(ApplicationEngine engine, string name, UInt160 admin)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string[] names = name.Split('.');
            if (names.Length != 2) throw new ArgumentException(null, nameof(name));
            if (admin != null && !engine.CheckWitnessInternal(admin)) throw new InvalidOperationException();
            NameState state = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Token).Add(GetKey(Utility.StrictUTF8.GetBytes(name)))).GetInteroperable<NameState>();
            if (!engine.CheckWitnessInternal(state.Owner)) throw new InvalidOperationException();
            state.Admin = admin;
        }

        private static bool CheckAdmin(ApplicationEngine engine, NameState state)
        {
            if (engine.CheckWitnessInternal(state.Owner)) return true;
            if (state.Admin is null) return false;
            return engine.CheckWitnessInternal(state.Admin);
        }

        [ContractMethod(CpuFee = 1 << 15, StorageFee = 200, RequiredCallFlags = CallFlags.States)]
        private void SetRecord(ApplicationEngine engine, string name, RecordType type, string data)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            switch (type)
            {
                case RecordType.A:
                    if (!ipv4Regex.IsMatch(data)) throw new FormatException();
                    if (!IPAddress.TryParse(data, out IPAddress address)) throw new FormatException();
                    if (address.AddressFamily != AddressFamily.InterNetwork) throw new FormatException();
                    break;
                case RecordType.CNAME:
                    if (!nameRegex.IsMatch(data)) throw new FormatException();
                    break;
                case RecordType.TXT:
                    if (Utility.StrictUTF8.GetByteCount(data) > 255) throw new FormatException();
                    break;
                case RecordType.AAAA:
                    if (!ipv6Regex.IsMatch(data)) throw new FormatException();
                    if (!IPAddress.TryParse(data, out address)) throw new FormatException();
                    if (address.AddressFamily != AddressFamily.InterNetworkV6) throw new FormatException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
            string domain = string.Join('.', name.Split('.')[^2..]);
            byte[] hash_domain = GetKey(Utility.StrictUTF8.GetBytes(domain));
            NameState state = engine.Snapshot[CreateStorageKey(Prefix_Token).Add(hash_domain)].GetInteroperable<NameState>();
            if (!CheckAdmin(engine, state)) throw new InvalidOperationException();
            StorageItem item = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Record).Add(hash_domain).Add(GetKey(Utility.StrictUTF8.GetBytes(name))).Add(type), () => new StorageItem());
            item.Value = Utility.StrictUTF8.GetBytes(data);
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public string GetRecord(DataCache snapshot, string name, RecordType type)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string domain = string.Join('.', name.Split('.')[^2..]);
            byte[] hash_domain = GetKey(Utility.StrictUTF8.GetBytes(domain));
            StorageItem item = snapshot.TryGet(CreateStorageKey(Prefix_Record).Add(hash_domain).Add(GetKey(Utility.StrictUTF8.GetBytes(name))).Add(type));
            if (item is null) return null;
            return Utility.StrictUTF8.GetString(item.Value);
        }

        public IEnumerable<(RecordType Type, string Data)> GetRecords(DataCache snapshot, string name)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string domain = string.Join('.', name.Split('.')[^2..]);
            byte[] hash_domain = GetKey(Utility.StrictUTF8.GetBytes(domain));
            foreach (var (key, value) in snapshot.Find(CreateStorageKey(Prefix_Record).Add(hash_domain).Add(GetKey(Utility.StrictUTF8.GetBytes(name))).ToArray()))
                yield return ((RecordType)key.Key[^1], Utility.StrictUTF8.GetString(value.Value));
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.States)]
        private void DeleteRecord(ApplicationEngine engine, string name, RecordType type)
        {
            if (!nameRegex.IsMatch(name)) throw new ArgumentException(null, nameof(name));
            string domain = string.Join('.', name.Split('.')[^2..]);
            byte[] hash_domain = GetKey(Utility.StrictUTF8.GetBytes(domain));
            NameState state = engine.Snapshot[CreateStorageKey(Prefix_Token).Add(hash_domain)].GetInteroperable<NameState>();
            if (!CheckAdmin(engine, state)) throw new InvalidOperationException();
            engine.Snapshot.Delete(CreateStorageKey(Prefix_Record).Add(hash_domain).Add(GetKey(Utility.StrictUTF8.GetBytes(name))).Add(type));
        }

        [ContractMethod(CpuFee = 1 << 17, RequiredCallFlags = CallFlags.ReadStates)]
        public string Resolve(DataCache snapshot, string name, RecordType type)
        {
            return Resolve(snapshot, name, type, 2);
        }

        private string Resolve(DataCache snapshot, string name, RecordType type, int redirect)
        {
            if (redirect < 0) throw new InvalidOperationException();
            var dictionary = GetRecords(snapshot, name).ToDictionary(p => p.Type, p => p.Data);
            if (dictionary.TryGetValue(type, out string data)) return data;
            if (!dictionary.TryGetValue(RecordType.CNAME, out data)) return null;
            return Resolve(snapshot, data, type, redirect - 1);
        }

        public class NameState : NFTState
        {
            public uint Expiration;
            public UInt160 Admin;

            public override byte[] Id => Utility.StrictUTF8.GetBytes(Name);

            public override Map ToMap(ReferenceCounter referenceCounter)
            {
                Map map = base.ToMap(referenceCounter);
                map["expiration"] = Expiration;
                return map;
            }

            public override void FromStackItem(StackItem stackItem)
            {
                base.FromStackItem(stackItem);
                Struct @struct = (Struct)stackItem;
                Expiration = (uint)@struct[2].GetInteger();
                Admin = @struct[3].IsNull ? null : new UInt160(@struct[3].GetSpan());
            }

            public override StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                Struct @struct = (Struct)base.ToStackItem(referenceCounter);
                @struct.Add(Expiration);
                @struct.Add(Admin?.ToArray() ?? StackItem.Null);
                return @struct;
            }
        }

        private class StringList : List<string>, IInteroperable
        {
            void IInteroperable.FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (VM.Types.Array)stackItem)
                    Add(item.GetString());
            }

            StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
            {
                return new VM.Types.Array(referenceCounter, this.Select(p => (ByteString)p));
            }
        }
    }
}
