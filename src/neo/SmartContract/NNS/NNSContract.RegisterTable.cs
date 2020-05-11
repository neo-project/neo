using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.NNS
{
    partial class NnsContract
    {
        //set the operator of the name, only can called by the current owner
        [ContractMethod(0_03000000, ContractParameterType.Boolean, CallFlags.AllowModifyStates, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.Hash160 }, ParameterNames = new[] { "name", "manager" })]
        private StackItem SetOperator(ApplicationEngine engine, Array args)
        {
            byte[] tokenId = args[0].GetSpan().ToArray();
            UInt160 manager = new UInt160(args[1].GetSpan());
            string name = System.Text.Encoding.UTF8.GetString(tokenId).ToLower();
            if (IsRootDomain(name) || !IsDomain(name)) return false;
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageKey key = CreateTokenKey(innerKey);
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(key);
            if (storage is null) return false;
            DomainState domainInfo = storage.GetInteroperable<DomainState>();

            if (IsExpired(engine.Snapshot, innerKey)) return false;
            IEnumerator enumerator = OwnerOf(engine.Snapshot, tokenId);
            UInt160 owner = null;
            if (enumerator.MoveNext())
            {
                owner = (UInt160)enumerator.Current;
            }
            if (!engine.CallingScriptHash.Equals(owner) && (!InteropService.Runtime.CheckWitnessInternal(engine, owner))) return false;
            domainInfo.Operator = manager;
            return true;
        }

        protected internal override void Mint(ApplicationEngine engine, UInt160 account, byte[] tokenId)
        {
            base.Mint(engine, account, tokenId);
            UInt256 innerKey = GetInnerKey(tokenId);
            StorageKey token_key = CreateTokenKey(innerKey);
            StorageItem token_storage = engine.Snapshot.Storages.GetAndChange(token_key);
            DomainState domainInfo = token_storage.GetInteroperable<DomainState>();
            domainInfo.Name = System.Text.Encoding.UTF8.GetString(tokenId);
            domainInfo.Operator = account;
            domainInfo.TimeToLive = engine.Snapshot.Height + 2000000;
        }
    }
}
