using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Text;
using System.Linq;
using Array = Neo.VM.Types.Array;
using Neo.Cryptography;
using Neo.Persistence;
using System.Numerics;
using Neo.SmartContract.Native.Tokens;
using System.Collections;
using Neo.Network.P2P.Payloads;

namespace Neo.SmartContract.NNS
{
    partial class NnsContract : Nep11Token<DomainState, Nep11AccountState>
    {
        public const uint BlockPerYear = Blockchain.DecrementInterval;

        public override UInt256 GetInnerKey(byte[] parameter)
        {
            return ComputeNameHash(System.Text.Encoding.UTF8.GetString(parameter));
        }

        //Get all root names
        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates)]
        private StackItem GetRootName(ApplicationEngine engine, Array args)
        {
            return new InteropInterface(GetRootName(engine.Snapshot));
        }

        public IEnumerator GetRootName(StoreView snapshot)
        {
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Root).ToArray()).Select(p => System.Text.Encoding.UTF8.GetString(p.Value.ToArray())).GetEnumerator();
        }

        //register root name, only can be called by admin
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        public StackItem RegisterRootName(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = Encoding.UTF8.GetString(tokenId);

            if (!IsAdminCalling(engine)) return false;
            if (!IsRootDomain(name)) return false;
            UInt256 innerKey = ComputeNameHash(name);
            StorageKey key = CreateStorageKey(Prefix_Root, innerKey.ToArray());
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage != null) return false;
            engine.Snapshot.Storages.Add(key, new StorageItem() { Value = tokenId });
            IncreaseTotalSupply(engine);
            return true;
        }

        //update ttl of first level name, can by called by anyone
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "ttl" })]
        private StackItem RenewName(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = Encoding.UTF8.GetString(tokenId);
            if (!IsDomain(name)) return false;
            string[] names = name.Split(".");
            int level = names.Length;
            if (level != 2) return false;

            UInt256 innerKey = GetInnerKey(tokenId);
            uint validUntilBlock = (uint)args[1].GetBigInteger();
            ulong duration = validUntilBlock - engine.Snapshot.Height;
            if (duration < 0) return false;
            StorageKey key = CreateTokenKey(innerKey);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            if (storage is null) return false;
            DomainState domain_state = storage.GetInteroperable<DomainState>();
            domain_state.TimeToLive = validUntilBlock;
            BigInteger amount = duration * GetRentalPrice(engine.Snapshot) / BlockPerYear;
            return GAS.Transfer(engine, ((Transaction)engine.ScriptContainer).Sender, GetReceiptAddress(engine.Snapshot), (new BigDecimal(amount, 8)).Value);
        }

        public override bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenId)
        {
            if (!Factor.Equals(amount)) return false;
            string name = System.Text.Encoding.UTF8.GetString(tokenId);
            UInt256 innerKey = GetInnerKey(tokenId);
            if (IsRootDomain(name) || !IsDomain(name)) return false;

            string[] names = name.Split(".");
            int level = names.Length;
            if (level >= 5) return false;

            string parentDomain = string.Join(".", name.Split(".")[1..]);
            UInt256 parentInnerKey = GetInnerKey(System.Text.Encoding.UTF8.GetBytes(parentDomain));
            var domainInfo = GetDomainInfo(engine.Snapshot, innerKey);
            uint ttl = engine.Snapshot.Height + BlockPerYear;
            if (domainInfo is null)
            {
                if (IsCrossLevel(engine.Snapshot, name) || IsExpired(engine.Snapshot, parentInnerKey)) return false;
                var parentDomianOwner = OwnerOf(engine.Snapshot, System.Text.Encoding.UTF8.GetBytes(parentDomain)).Current;
                if (!parentDomianOwner.Equals(from)) return false;
                if (!InteropService.Runtime.CheckWitnessInternal(engine, from))
                    return false;
            }
            else
            {
                if (!IsExpired(engine.Snapshot, innerKey)) return base.Transfer(engine, from, to, Factor, tokenId);
                if (!IsRootDomain(parentDomain) && IsExpired(engine.Snapshot, parentInnerKey)) return false;
                var oldOwner = OwnerOf(engine.Snapshot, tokenId).Current;
                var parentDomianOwner = OwnerOf(engine.Snapshot, System.Text.Encoding.UTF8.GetBytes(parentDomain)).Current;
                if (!parentDomianOwner.Equals(from)) return false;
                if (!InteropService.Runtime.CheckWitnessInternal(engine, from))
                    return false;
                Burn(engine, (UInt160)oldOwner, Factor, tokenId);
            }
            ttl = engine.Snapshot.Storages.TryGet(CreateTokenKey(parentInnerKey))?.GetInteroperable<DomainState>().TimeToLive ?? ttl;
            Mint(engine, from, tokenId, ttl);
            return base.Transfer(engine, from, to, Factor, tokenId);
        }

        private DomainState GetDomainInfo(StoreView snapshot, UInt256 nameHash)
        {
            StorageKey key = CreateTokenKey(nameHash);
            StorageItem storage = snapshot.Storages.TryGet(key);
            return storage?.GetInteroperable<DomainState>();
        }

        private bool IsCrossLevel(StoreView snapshot, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string fatherLevel = string.Join(".", name.Split(".")[1..]);
            UInt256 innerKey = ComputeNameHash(fatherLevel);
            var domainInfo = GetDomainInfo(snapshot, innerKey);
            if (domainInfo is null) return true;
            return false;
        }

        private UInt256 ComputeNameHash(string name)
        {
            return new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
        }

        private bool IsAdminCalling(ApplicationEngine engine)
        {
            if (!InteropService.Runtime.CheckWitnessInternal(engine, GetAdmin(engine.Snapshot))) return false;
            return true;
        }
    }
}
