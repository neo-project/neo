using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Nns
{
    public partial class NnsContract : Nep11Token<DomainState, Nep11AccountState>
    {
        public override int Id => -5;
        public override string Name => "NNS";
        public override string Symbol => "nns";
        public override byte Decimals => 0;

        private const uint BlockPerYear = Blockchain.DecrementInterval;
        private const uint MaxDomainLevel = 4;
        private const uint MaxResolveCount = 7;

        private const byte Prefix_Root = 24;
        private const byte Prefix_Record = 25;


        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Root), new StorageItem(new RootDomainState()
            {
                Roots = new Array() { "neo", "wallet", "dapp" }
            }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates)]
        public StackItem GetRootName(ApplicationEngine engine, Array args)
        {
            return engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Root)).GetInteroperable<RootDomainState>().Roots;
        }

        [ContractMethod(1_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "account" })]
        public StackItem Register(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = Encoding.UTF8.GetString(tokenId).ToLower();
            string[] domains = name.Split(".");
            string root = domains[domains.Length - 1];
            if (domains.Length - 1 > MaxDomainLevel) throw new InvalidOperationException("Out of max domain level 4");

            var storages = engine.Snapshot.Storages;
            RootDomainState rootState = storages.GetAndChange(CreateStorageKey(Prefix_Root)).GetInteroperable<RootDomainState>();
            if (domains.Length == 1)
            {
                if (rootState.Contains(root)) return false;
                if (!InteropService.Runtime.CheckWitnessInternal(engine, NEO.GetCommitteeMultiSigAddress(engine.Snapshot))) return false;
                rootState.Roots.Add(root);
                return true;
            }
            else
            {
                if (!rootState.Contains(root)) throw new InvalidOperationException("The root domain is invalid");

                UInt160 account = args[1].GetSpan().AsSerializable<UInt160>();
                DomainState domainState = GetDomainInfo(engine.Snapshot, tokenId);
                var ttl = BlockPerYear;
                if (domainState != null)
                {
                    if (domainState.IsExpired(engine.Snapshot))
                        Burn(engine, tokenId);

                    string parentDomain = string.Join(".", name.Split(".")[1..]);
                    if (IsRootDomain(parentDomain) || IsCrossLevel(engine.Snapshot, name)) return false;

                    byte[] parentTokenId = Encoding.UTF8.GetBytes(parentDomain);
                    var parentOwner = GetOwner(engine.Snapshot, parentTokenId);
                    var parentDomainState = GetDomainInfo(engine.Snapshot, parentTokenId);
                    if (parentDomainState is null || parentDomainState.IsExpired(engine.Snapshot)) return false;
                    if (!InteropService.Runtime.CheckWitnessInternal(engine, parentOwner)) return false;
                    ttl = parentDomainState.TimeToLive;
                }
                Mint(engine, account, tokenId, ttl);
            }
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        public StackItem RenewName(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = Encoding.UTF8.GetString(tokenId).ToLower();
            if (name.Split(".").Length != 2) return false; // only the first-level name can be renew

            DomainState domainState = GetDomainInfo(engine.Snapshot, tokenId, true);
            if (domainState is null) return false;
            if (domainState.IsExpired(engine.Snapshot))
                domainState.TimeToLive = engine.Snapshot.Height + BlockPerYear;
            else
                domainState.TimeToLive += BlockPerYear;
            return true;
        }

        public override bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenId)
        {
            if (!Factor.Equals(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
            var domainInfo = GetDomainInfo(engine.Snapshot, tokenId);
            if (domainInfo is null) return false;
            if (domainInfo.IsExpired(engine.Snapshot)) throw new InvalidOperationException("Name is expired");

            return base.Transfer(engine, from, to, Factor, tokenId);
        }

        // only can be called by the admin of the name
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Integer, ContractParameterType.String }, ParameterNames = new[] { "name", "recordType", "text" })]
        private StackItem SetText(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            RecordType recordType = (RecordType)(byte)args[1].GetBigInteger();
            byte[] text = args[2].GetSpan().ToArray();
            switch (recordType)
            {
                case RecordType.A:
                    if (text.Length != UInt160.Length) return false;
                    break;
                case RecordType.CNAME:
                    string cname = Encoding.UTF8.GetString(text);
                    if (!IsDomain(cname)) return false;
                    break;
            }
            UInt160 owner = GetOwner(engine.Snapshot, tokenId);
            if (owner is null) return false;
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId, true);
            if (domainInfo.IsExpired(engine.Snapshot)) return false;
            if (!InteropService.Runtime.CheckWitnessInternal(engine, owner)) return false;
            domainInfo.Type = recordType;
            domainInfo.Text = text;
            return true;
        }

        // return the text and recordtype of the name
        [ContractMethod(0_03000000, ContractParameterType.String, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        public StackItem Resolve(ApplicationEngine engine, Array args)
        {
            byte[] name = args[0].GetSpan().ToArray();
            return Resolve(engine.Snapshot, name).ToStackItem(engine.ReferenceCounter);
        }

        public DomainState Resolve(StoreView snapshot, byte[] domain, int resolveCount = 0)
        {
            if (resolveCount > MaxResolveCount)
                return new DomainState { Type = RecordType.ERROR, Text = Encoding.ASCII.GetBytes("Too many domain redirects") };

            DomainState domainInfo = GetDomainInfo(snapshot, domain);
            if (domainInfo is null || domainInfo.IsExpired(snapshot))
                return new DomainState { Type = RecordType.ERROR, Text = Encoding.ASCII.GetBytes("Domain not found or expired") };

            UInt160 innerKey = GetInnerKey(domain);
            StorageKey key = CreateStorageKey(Prefix_Record, innerKey);
            StorageItem storage = snapshot.Storages.TryGet(key);
            if (storage is null)
                return new DomainState { Type = RecordType.ERROR, Text = Encoding.ASCII.GetBytes("Text does not exist") };

            DomainState recordInfo = storage.GetInteroperable<DomainState>();
            switch (recordInfo.Type)
            {
                case RecordType.CNAME:
                    var parameter_cname = recordInfo.Text;
                    return Resolve(snapshot, parameter_cname, ++resolveCount);
            }
            return recordInfo;
        }

        protected internal void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId, uint ttl)
        {
            base.Mint(engine, account, tokenId);
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId, true);
            domainInfo.TimeToLive = ttl;
            domainInfo.Type = RecordType.A;
            domainInfo.Text = account.ToArray();
        }

        private DomainState GetDomainInfo(StoreView snapshot, byte[] tokenid, bool update = false)
        {
            UInt160 innerKey = GetInnerKey(tokenid);
            StorageKey key = CreateTokenKey(innerKey);
            if (update)
                return snapshot.Storages.GetAndChange(key)?.GetInteroperable<DomainState>();
            else
                return snapshot.Storages.TryGet(key)?.GetInteroperable<DomainState>();
        }

        private UInt160 GetOwner(StoreView snapshot, byte[] tokenid)
        {
            IEnumerator enumerator = OwnerOf(snapshot, tokenid);
            if (!enumerator.MoveNext()) return null;
            return (UInt160)enumerator.Current;
        }

        private bool IsCrossLevel(StoreView snapshot, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string fatherLevel = string.Join(".", name.Split(".")[1..]);
            byte[] tokenId = Encoding.UTF8.GetBytes(fatherLevel);
            if (IsRootDomain(fatherLevel))
            {
                UInt160 innerKey = GetInnerKey(tokenId);
                return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Root, innerKey)) == null;
            }
            var domainInfo = GetDomainInfo(snapshot, tokenId);
            return (domainInfo is null);
        }

        public bool IsDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string pattern = @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62}){1,3}$";
            Regex regex = new Regex(pattern);
            return regex.Match(name).Success;
        }

        public bool IsRootDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string pattern = @"^[a-zA-Z]{0,62}$";
            Regex regex = new Regex(pattern);
            return regex.Match(name).Success;
        }
    }

    internal class RootDomainState : IInteroperable
    {
        public Array Roots;

        public void FromStackItem(StackItem stackItem)
        {
            Roots = (Array)stackItem;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return Roots;
        }

        public bool Contains(string domain)
        {
            foreach (var root in Roots)
                if (root.Equals((StackItem)domain)) return true;
            return false;
        }
    }
}
