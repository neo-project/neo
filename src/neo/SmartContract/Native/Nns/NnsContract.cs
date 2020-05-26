using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections;
using System.Linq;
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

        private const byte Prefix_Roots = 24;

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Roots), new StorageItem(new RootDomainState()
            {
                Roots = new Array() { "neo", "wallet", "dapp", "org" }
            }));
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates)]
        public StackItem GetRoots(ApplicationEngine engine, Array args)
        {
            return engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Roots)).GetInteroperable<RootDomainState>().Roots;
        }

        [ContractMethod(1_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "owner" })]
        private StackItem Register(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = Encoding.UTF8.GetString(tokenId).ToLower();
            string[] domains = name.Split(".");
            if (domains.Length > 3) return false; // only the root and first-level name can be registered

            string root = domains[^1];
            RootDomainState rootState = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Roots)).GetInteroperable<RootDomainState>();
            if (domains.Length == 1)
            {
                if (rootState.Contains(root) || !IsRoot(root)) return false;
                if (!InteropService.Runtime.CheckWitnessInternal(engine, NEO.GetCommitteeMultiSigAddress(engine.Snapshot))) return false;
                rootState.Roots.Add(root);
            }
            else
            {
                if (!rootState.Contains(root) || !IsDomain(name)) return false;

                DomainState domainState = GetDomainInfo(engine.Snapshot, tokenId);
                if (domainState != null)
                {
                    if (!domainState.IsExpired(engine.Snapshot))
                        return false;
                    Burn(engine, tokenId);
                }

                var ttl = engine.Snapshot.Height + BlockPerYear;
                UInt160 owner = args[1].GetSpan().AsSerializable<UInt160>();
                Mint(engine, owner, tokenId, ttl);
            }
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        private StackItem RenewName(ApplicationEngine engine, Array args)
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
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId, true);
            if (domainInfo is null || domainInfo.IsExpired(engine.Snapshot)) return false;
            if (!InteropService.Runtime.CheckWitnessInternal(engine, domainInfo.Operator)) return false;
            domainInfo.Type = recordType;
            domainInfo.Text = text;
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "tokenid", "operator" })]
        private StackItem SetOperator(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            UInt160 @operator = args[1].GetSpan().AsSerializable<UInt160>();

            var owner = GetOwner(engine.Snapshot, tokenId);
            if (owner is null || !InteropService.Runtime.CheckWitnessInternal(engine, owner)) return false;
            DomainState domainState = GetDomainInfo(engine.Snapshot, tokenId, true);
            domainState.Operator = @operator;
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.String, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        private StackItem Resolve(ApplicationEngine engine, Array args)
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

            switch (domainInfo.Type)
            {
                case RecordType.CNAME:
                    var parameter_cname = domainInfo.Text;
                    return Resolve(snapshot, parameter_cname, ++resolveCount);
            }
            return domainInfo;
        }

        public override bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenId)
        {
            if (!Factor.Equals(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
            var domainInfo = GetDomainInfo(engine.Snapshot, tokenId);
            if (domainInfo != null)
            {
                if (domainInfo.IsExpired(engine.Snapshot))
                    Burn(engine, tokenId);
                else
                    return base.Transfer(engine, from, to, Factor, tokenId);
            }

            string name = Encoding.UTF8.GetString(tokenId).ToLower();
            if (!IsDomain(name)) return false;
            string[] domains = name.Split(".");
            if (domains.Length > MaxDomainLevel) return false;
            if (domains.Length <= 2) return false; // The first-level must be registered

            for (var i = 1; i < domains.Length - 1; i++) // Sub domain can be created directly by the owner of parent domain name
            {
                string parentDomain = string.Join(".", domains.Skip(i));
                byte[] parentTokenId = Encoding.UTF8.GetBytes(parentDomain);
                var parentState = GetDomainInfo(engine.Snapshot, parentTokenId);
                if (parentState is null) continue;
                if (parentState.IsExpired(engine.Snapshot))
                {
                    Burn(engine, parentTokenId);
                    continue;
                }

                var parentOwner = GetOwner(engine.Snapshot, parentTokenId);
                if (!parentOwner.Equals(from) || !InteropService.Runtime.CheckWitnessInternal(engine, parentOwner)) return false;

                var ttl = parentState.TimeToLive;
                Mint(engine, parentOwner, tokenId, ttl);
                return base.Transfer(engine, from, to, Factor, tokenId);
            }
            return false;
        }

        public override JObject Properties(StoreView snapshot, byte[] tokenid)
        {
            JObject json = new JObject();
            DomainState domain = GetDomainInfo(snapshot, tokenid);
            if (domain is null || domain.IsExpired(snapshot))
                return json;
            json["name"] = Encoding.ASCII.GetString(domain.TokenId);
            json["description"] = domain.Text.ToHexString();
            return json;
        }

        protected internal void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId, uint ttl)
        {
            base.Mint(engine, account, tokenId);
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId, true);
            domainInfo.Operator = account;
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

        public bool IsDomain(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string pattern = @"^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62}){1,3}$";
            Regex regex = new Regex(pattern);
            return regex.Match(name).Success;
        }

        public bool IsRoot(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string pattern = @"^[a-zA-Z]{0,62}$";
            Regex regex = new Regex(pattern);
            return regex.Match(name).Success;
        }
    }

    public class DomainState : Nep11TokenState
    {
        public UInt160 Operator { set; get; }
        public uint TimeToLive { set; get; }
        public RecordType Type { set; get; }
        public byte[] Text { get; set; }

        public override void FromStackItem(StackItem stackItem)
        {
            base.FromStackItem(stackItem);
            Struct @struct = (Struct)stackItem;
            Operator = @struct[1].GetSpan().AsSerializable<UInt160>();
            TimeToLive = (uint)@struct[2].GetBigInteger();
            Type = (RecordType)@struct[3].GetSpan().ToArray()[0];
            Text = @struct[4].GetSpan().ToArray();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = (Struct)base.ToStackItem(referenceCounter);
            @struct.Add(Operator.ToArray());
            @struct.Add(TimeToLive);
            @struct.Add(new byte[] { (byte)Type });
            @struct.Add(Text);
            return @struct;
        }

        public bool IsExpired(StoreView snapshot)
        {
            return snapshot.Height.CompareTo(TimeToLive) > 0;
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
