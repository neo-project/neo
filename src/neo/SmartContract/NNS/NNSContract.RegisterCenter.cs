using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Array = Neo.VM.Types.Array;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Persistence;
using System.Numerics;
using Neo.SmartContract.Native.Tokens;
using System.Collections;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract : Nep11Token<DomainState>
    {
        public override BigInteger BalanceOf(StoreView snapshot, UInt160 account, byte[] tokenid)
        {
            UInt256 namehash = ComputeNameHash(System.Text.Encoding.UTF8.GetString(tokenid));
            return base.BalanceOf(snapshot, account, namehash.ToArray());
        }

        public override IEnumerator OwnerOf(StoreView snapshot, byte[] tokenid)
        {
            UInt256 namehash = ComputeNameHash(System.Text.Encoding.UTF8.GetString(tokenid));
            return base.OwnerOf(snapshot, namehash.ToArray());
        }

        public override IEnumerator TokensOf(StoreView snapshot, UInt160 owner)
        {
            UInt256[] domains = snapshot.Storages[CreateStorageKey(Prefix_OwnershipMapping, owner)].Value.AsSerializableArray<UInt256>();
            return domains.ToList().Select(p => snapshot.Storages[CreateStorageKey(Prefix_tokenid, p)]?.Value.AsSerializable<DomainState>().Name).ToList().GetEnumerator();
        }

        //Get all root names
        [ContractMethod(0_01000000, ContractParameterType.Array, CallFlags.AllowStates)]
        private StackItem GetRootName(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetRootName(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        public UInt256[] GetRootName(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_Root)].Value.AsSerializableArray<UInt256>();
        }

        //register root name, only can be called by admin
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String }, ParameterNames = new[] { "name" })]
        public StackItem RegisterRootName(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = ComputeNameHash(name);

            if (IsRootDomain(name))
            {
                if (!IsAdminCalling(engine)) return false;

                StorageKey key = CreateStorageKey(Prefix_Root);
                StorageItem storage = engine.Snapshot.Storages[key];
                SortedSet<UInt256> roots = new SortedSet<UInt256>(storage.Value.AsSerializableArray<UInt256>());
                if (!roots.Add(nameHash)) return false;
                storage = engine.Snapshot.Storages.GetAndChange(key);
                storage.Value = roots.ToByteArray();
                StorageItem storage_totalSupply = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TotalSupply), () => new StorageItem() { Value = BigInteger.Zero.ToByteArray() });
                storage_totalSupply.Value = (new BigInteger(storage_totalSupply.Value) + BigInteger.One).ToByteArray();
                return true;
            }
            return false;
        }

        //update ttl of first level name, can by called by anyone
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.String, ContractParameterType.Integer }, ParameterNames = new[] { "name", "ttl" })]
        private StackItem RenewName(ApplicationEngine engine, Array args)
        {
            string name = args[0].GetString().ToLower();
            UInt256 nameHash = ComputeNameHash(name);
            uint validUntilBlock = (uint)args[1].GetBigInteger();
            ulong duration = validUntilBlock - engine.Snapshot.Height;
            if (duration < 0) return false;

            if (IsDomain(name))
            {
                StorageKey key = CreateStorageKey(Prefix_tokenid, nameHash);
                StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
                DomainState domainInfo = storage.Value.AsSerializable<DomainState>();
                if (domainInfo is null) return false;
                domainInfo.TimeToLive = validUntilBlock;
                storage = engine.Snapshot.Storages.GetAndChange(key);
                storage.Value = domainInfo.ToArray();
                UInt160 owner = domainInfo.owners.GetEnumerator().Current.Key;
                uint blocksPerYear = 200;
                BigInteger amount = duration * GetRentalPrice(engine.Snapshot) / blocksPerYear;
                return PolicyContract.NEO.Transfer(engine, owner, GetReceiptAddress(engine.Snapshot), (new BigDecimal(amount, 8)).Value);
            }
            return false;
        }

        protected override bool Transfer(ApplicationEngine engine, UInt160 from, UInt160 to, BigInteger amount, byte[] tokenid)
        {
            if (!Factor.Equals(amount)) return false;
            string name = System.Text.Encoding.UTF8.GetString(tokenid);
            UInt256 nameHash = ComputeNameHash(name);
            if (IsRootDomain(name) || !IsDomain(name)) return false;

            // check whether the registration is cross-level 
            string[] names = name.Split(".");
            if (names.Length >= 5) return false;
            string secondLevel = names.Length >= 3 ? string.Join(".", names[^3..]) : null;
            string thirdLevel = names.Length == 4 ? name : null;
            if (isCrossLevel(engine.Snapshot, secondLevel) || isCrossLevel(engine.Snapshot, thirdLevel))
                return false;

            var domainInfo = GetDomainInfo(engine.Snapshot, nameHash);
            if (domainInfo != null)
            {
                if (IsExpired(engine.Snapshot, nameHash))
                {
                    ECPoint[] admins = GetAdmin(engine.Snapshot);
                    UInt160 admin = Contract.CreateMultiSigRedeemScript(admins.Length - (admins.Length - 1) / 3, admins).ToScriptHash();
                    RecoverDomainState(engine, name, admin);
                }
                base.Transfer(engine, from, to, amount, nameHash.ToArray());
            }
            else
            {
                CreateNewDomain(engine, name, from);
                base.Transfer(engine, from, to, amount, nameHash.ToArray());
            }
            engine.SendNotification(Hash, new Array(new StackItem[] { "Transfer", from.ToArray(), to.ToArray(), amount, name }));
            return true;
        }

        private DomainState GetDomainInfo(StoreView snapshot, UInt256 nameHash)
        {
            StorageKey key = CreateStorageKey(Prefix_tokenid, nameHash);
            StorageItem storage = snapshot.Storages.TryGet(key);
            if (storage is null) return null;
            return storage.Value.AsSerializable<DomainState>();
        }

        private bool isCrossLevel(StoreView snapshot, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string fatherLevel = string.Join(".", name.Split(".")[1..]);
            UInt256 nameHash = ComputeNameHash(fatherLevel);
            var domainInfo = GetDomainInfo(snapshot, nameHash);
            if (domainInfo is null) return true;
            return false;
        }

        private UInt256 ComputeNameHash(string name)
        {
            return new UInt256(Crypto.Hash256(Encoding.UTF8.GetBytes(name)));
        }

        private bool IsAdminCalling(ApplicationEngine engine)
        {
            ECPoint[] admins = GetAdmin(engine.Snapshot);
            UInt160 script = Contract.CreateMultiSigRedeemScript(admins.Length - (admins.Length - 1) / 3, admins).ToScriptHash();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, script)) return false;
            return true;
        }
    }
}
