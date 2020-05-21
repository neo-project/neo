using Neo.IO;
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
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Nns
{
    public partial class NnsContract : Nep11Token<DomainState, Nep11AccountState>
    {
        public override int Id => -5;
        public override string Name => "NNS";
        public override string Symbol => "nns";
        public override byte Decimals => 0;

        public const uint BlockPerYear = Blockchain.DecrementInterval;
        public const uint MaxDomainLevel = 4;

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Admin), new StorageItem
            {
                Value = NEO.GetCommitteeMultiSigAddress(engine.Snapshot).ToArray()
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_ReceiptAddress), new StorageItem
            {
                Value = NEO.GetCommitteeMultiSigAddress(engine.Snapshot).ToArray()
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_RentalPrice), new StorageItem
            {
                Value = BitConverter.GetBytes(5_000_000_000L)
            });
            return true;
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
            if (!IsRootDomain(name) || !IsAdminCalling(engine)) return false;

            UInt160 innerKey = GetInnerKey(tokenId);
            StorageKey key = CreateRootKey(innerKey);
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
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
            if (validUntilBlock <= engine.Snapshot.Height) return false;

            DomainState domainState = GetDomainInfo(engine.Snapshot, tokenId, true);
            if (domainState is null) return false;
            string name = Encoding.UTF8.GetString(tokenId).ToLower();
            int level = name.Split(".").Length;
            if (level != 2) return false;

            BigInteger amount = (validUntilBlock - engine.Snapshot.Height) * GetRentalPrice(engine.Snapshot) / BlockPerYear;
            if (!GAS.Transfer(engine, from, GetReceiptAddress(engine.Snapshot), amount)) return false;

            domainState.TimeToLive = validUntilBlock;
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
                byte[] parentTokenId = Encoding.UTF8.GetBytes(parentDomain);
                parentOwner = GetOwner(engine.Snapshot, parentTokenId);
                var parentDomainState = GetDomainInfo(engine.Snapshot, parentTokenId);

                if (IsCrossLevel(engine.Snapshot, name)) return false;
                if (parentDomainState is null || parentDomainState.IsExpired(engine.Snapshot)) return false;
                ttl = parentDomainState.TimeToLive;
            }
            if (!parentOwner.Equals(from) || !InteropService.Runtime.CheckWitnessInternal(engine, from)) return false;

            Mint(engine, from, tokenId, ttl);
            return base.Transfer(engine, from, to, Factor, tokenId);
        }

        //set the operator of the name, only can called by the current owner
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "operator" })]
        public StackItem SetOperator(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            UInt160 @operator = new UInt160(args[1].GetSpan());
            UInt160 owner = GetOwner(engine.Snapshot, tokenId);
            if (!InteropService.Runtime.CheckWitnessInternal(engine, owner)) return false;

            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId, true);
            if (domainInfo is null || domainInfo.IsExpired(engine.Snapshot)) return false;
            domainInfo.Operator = @operator;
            return true;
        }

        protected internal void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId, uint ttl)
        {
            base.Mint(engine, account, tokenId);
            DomainState domainInfo = GetDomainInfo(engine.Snapshot, tokenId);
            domainInfo.Operator = account;
            domainInfo.TimeToLive = ttl;
        }

        private StorageKey CreateRootKey(UInt160 innerKey)
        {
            return CreateStorageKey(Prefix_Root, innerKey.ToArray());
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
            if (domainInfo is null) return true;
            return false;
        }

        private bool IsAdminCalling(ApplicationEngine engine)
        {
            return InteropService.Runtime.CheckWitnessInternal(engine, GetAdmin(engine.Snapshot));
        }
    }
}
