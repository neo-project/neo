using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Collections;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Nns
{
    partial class NnsContract
    {
        //set the operator of the name, only can called by the current owner
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "operator" })]
        public StackItem SetOperator(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            UInt160 @operator = new UInt160(args[1].GetSpan());
            string name = System.Text.Encoding.UTF8.GetString(tokenId).ToLower();
            if (IsRootDomain(name) || !IsDomain(name)) return false;
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageKey key = CreateTokenKey(innerKey);
            DomainState domainInfo = engine.Snapshot.Storages.TryGet(key)?.GetInteroperable<DomainState>();
            if (domainInfo is null)
            {
                string parentDomain = string.Join(".", name.Split(".")[1..]);
                UInt256 parentInnerKey = GetInnerKey(System.Text.Encoding.UTF8.GetBytes(parentDomain));
                if (IsCrossLevel(engine.Snapshot, name) || IsExpired(engine.Snapshot, parentInnerKey)) return false;
                var parentDomianOwner = GetAdmin(engine.Snapshot);
                if (!IsRootDomain(parentDomain))
                {
                    IEnumerator enumerator = OwnerOf(engine.Snapshot, System.Text.Encoding.UTF8.GetBytes(parentDomain));
                    if (!enumerator.MoveNext()) return false;
                    parentDomianOwner = (UInt160)enumerator.Current;
                }
                if (!InteropService.Runtime.CheckWitnessInternal(engine, parentDomianOwner))
                    return false;
                uint ttl = engine.Snapshot.Height + BlockPerYear;
                ttl = engine.Snapshot.Storages.TryGet(CreateTokenKey(parentInnerKey))?.GetInteroperable<DomainState>().TimeToLive ?? ttl;
                Mint(engine, parentDomianOwner, tokenId, ttl);
            }
            else
            {
                if (IsExpired(engine.Snapshot, innerKey)) return false;
                IEnumerator enumerator = OwnerOf(engine.Snapshot, System.Text.Encoding.UTF8.GetBytes(name));
                if (!enumerator.MoveNext()) return false;
                var owner = (UInt160)enumerator.Current;
                if (!InteropService.Runtime.CheckWitnessInternal(engine, owner))
                    return false;
            }
            domainInfo = engine.Snapshot.Storages.GetAndChange(key).GetInteroperable<DomainState>();
            domainInfo.Operator = @operator;
            return true;
        }

        protected internal void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId, uint ttl)
        {
            Mint(engine, account, tokenId);
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageKey token_key = CreateTokenKey(innerKey);
            StorageItem token_storage = engine.Snapshot.Storages.GetAndChange(token_key);
            DomainState domainInfo = token_storage.GetInteroperable<DomainState>();
            domainInfo.Name = System.Text.Encoding.UTF8.GetString(tokenId);
            domainInfo.Operator = account;
            domainInfo.TimeToLive = ttl;
        }
    }
}
