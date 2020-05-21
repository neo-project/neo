using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Cryptography;
using Neo.Persistence;
using System;
using System.Text;
using System.Linq;
using System.Numerics;
using System.Collections;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Nns
{
    partial class NnsContract
    {
        public const uint BlockPerYear = Blockchain.DecrementInterval;
        public const uint MaxDomainLevel = 4;

        public override UInt256 GetInnerKey(byte[] parameter)
        {
            return ComputeNameHash(Encoding.UTF8.GetString(parameter).ToLower());
        }

        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates)]
        public StackItem GetRootName(ApplicationEngine engine, Array args)
        {
            return new InteropInterface(GetRootName(engine.Snapshot));
        }

        public IEnumerator GetRootName(StoreView snapshot)
        {
            return snapshot.Storages.Find(CreateStorageKey(Prefix_Root).ToArray()).Select(p => Encoding.UTF8.GetString(p.Value.Value.ToArray())).GetEnumerator();
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray }, ParameterNames = new[] { "name" })]
        public StackItem RegisterRootName(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            string name = Encoding.UTF8.GetString(tokenId);
            //Check name format and witness
            if (!IsRootDomain(name) || !IsAdminCalling(engine)) return false;
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageKey key = CreateRootKey(innerKey);
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            //Root name can't be duplicate
            if (storage != null) return false;
            engine.Snapshot.Storages.Add(key, new StorageItem() { Value = tokenId });
            IncreaseTotalSupply(engine.Snapshot);
            return true;
        }

        //update ttl of first level name, can by called by anyone
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Integer, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "ttl" })]
        public StackItem RenewName(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            uint validUntilBlock = (uint)args[1].GetBigInteger();
            UInt160 from = args[2].GetSpan().AsSerializable<UInt160>();
            string name = Encoding.UTF8.GetString(tokenId).ToLower();
            if (!IsDomain(name)) return false;
            string[] names = name.Split(".");
            int level = names.Length;
            if (level != 2) return false;

            UInt256 innerKey = GetInnerKey(tokenId);
            ulong duration = validUntilBlock - engine.Snapshot.Height;
            if (duration < 0) return false;
            StorageKey key = CreateTokenKey(innerKey);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            if (storage is null) return false;
            DomainState domain_state = storage.GetInteroperable<DomainState>();
            BigInteger amount = duration * GetRentalPrice(engine.Snapshot) / BlockPerYear;
            if (!GAS.Transfer(engine, from, GetReceiptAddress(engine.Snapshot), amount)) return false;
            domain_state.TimeToLive = validUntilBlock;
            return true;
        }

        public override bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenId)
        {
            if (!Factor.Equals(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
            string name = Encoding.UTF8.GetString(tokenId);
            int level = name.Split(".").Length;
            if (level > MaxDomainLevel || IsRootDomain(name) || !IsDomain(name)) return false;

            var domainInfo = GetDomainInfo(engine.Snapshot, tokenId);
            if (domainInfo != null)
            {
                if (domainInfo.IsExpired(engine.Snapshot))
                    Burn(engine, from, Factor, tokenId);
                else
                    return base.Transfer(engine, from, to, Factor, tokenId);
            }

            string parentDomain = string.Join(".", name.Split(".")[1..]);
            var parentOwner = GetAdmin(engine.Snapshot);
            var ttl = engine.Snapshot.Height + BlockPerYear;
            if (!IsRootDomain(parentDomain))
            {
                parentOwner = GetOwner(engine.Snapshot, parentDomain);
                byte[] parentTokenId = Encoding.UTF8.GetBytes(parentDomain);
                var parentDomainState = GetDomainInfo(engine.Snapshot, parentTokenId);

                if (IsCrossLevel(engine.Snapshot, name)) return false;
                if (parentDomainState is null || parentDomainState.IsExpired(engine.Snapshot)) return false;
                ttl = parentDomainState.TimeToLive;
            }
            if (!parentOwner.Equals(from) || !InteropService.Runtime.CheckWitnessInternal(engine, from)) return false;

            Mint(engine, from, tokenId, ttl);
            return base.Transfer(engine, from, to, Factor, tokenId);
        }

        private StorageKey CreateRootKey(UInt256 innerKey)
        {
            return CreateStorageKey(Prefix_Root, innerKey.ToArray());
        }

        private DomainState GetDomainInfo(StoreView snapshot, byte[] tokenid)
        {
            UInt256 innerKey = GetInnerKey(tokenid);
            StorageKey key = CreateTokenKey(innerKey);
            StorageItem storage = snapshot.Storages.TryGet(key);
            return storage?.GetInteroperable<DomainState>();
        }

        private UInt160 GetOwner(StoreView snapshot, string name)
        {
            IEnumerator enumerator = OwnerOf(snapshot, Encoding.UTF8.GetBytes(name));
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
                UInt256 innerKey = GetInnerKey(tokenId);
                return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Root, innerKey)) == null;
            }
            var domainInfo = GetDomainInfo(snapshot, tokenId);
            if (domainInfo is null) return true;
            return false;
        }

        private UInt256 ComputeNameHash(string name)
        {
            return new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name.ToLower())));
        }

        private bool IsAdminCalling(ApplicationEngine engine)
        {
            return InteropService.Runtime.CheckWitnessInternal(engine, GetAdmin(engine.Snapshot));
        }
    }
}
