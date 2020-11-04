#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native.Designate;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native.Oracle
{
    public sealed class OracleContract : NativeContract
    {
        private const int MaxUrlLength = 256;
        private const int MaxFilterLength = 128;
        private const int MaxCallbackLength = 32;
        private const int MaxUserDataLength = 512;

        private const byte Prefix_RequestId = 9;
        private const byte Prefix_Request = 7;
        private const byte Prefix_IdList = 6;

        private const long OracleRequestPrice = 0_01666667;

        public override int Id => -4;
        public override string Name => "Oracle";

        internal OracleContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        [ContractMethod(0, CallFlags.AllowModifyStates)]
        private void Finish(ApplicationEngine engine)
        {
            Transaction tx = (Transaction)engine.ScriptContainer;
            OracleResponse response = tx.GetAttribute<OracleResponse>();
            if (response == null) throw new ArgumentException("Oracle response was not found");
            OracleRequest request = GetRequest(engine.Snapshot, response.Id);
            if (request == null) throw new ArgumentException("Oracle request was not found");
            StackItem userData = BinarySerializer.Deserialize(request.UserData, engine.Limits.MaxStackSize, engine.Limits.MaxItemSize, engine.ReferenceCounter);
            engine.CallFromNativeContract(null, request.CallbackContract, request.CallbackMethod, request.Url, userData, (int)response.Code, response.Result);
        }

        private UInt256 GetOriginalTxid(ApplicationEngine engine)
        {
            Transaction tx = (Transaction)engine.ScriptContainer;
            OracleResponse response = tx.GetAttribute<OracleResponse>();
            if (response is null) return tx.Hash;
            OracleRequest request = GetRequest(engine.Snapshot, response.Id);
            return request.OriginalTxid;
        }

        public OracleRequest GetRequest(StoreView snapshot, ulong id)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Request).Add(id))?.GetInteroperable<OracleRequest>();
        }

        public IEnumerable<(ulong, OracleRequest)> GetRequests(StoreView snapshot)
        {
            return snapshot.Storages.Find(new KeyBuilder(Id, Prefix_Request).ToArray()).Select(p => (BitConverter.ToUInt64(p.Key.Key, 1), p.Value.GetInteroperable<OracleRequest>()));
        }

        public IEnumerable<OracleRequest> GetRequestsByUrl(StoreView snapshot, string url)
        {
            IdList list = snapshot.Storages.TryGet(CreateStorageKey(Prefix_IdList).Add(GetUrlHash(url)))?.GetInteroperable<IdList>();
            if (list is null) yield break;
            foreach (ulong id in list)
                yield return snapshot.Storages[CreateStorageKey(Prefix_Request).Add(id)].GetInteroperable<OracleRequest>();
        }

        private static byte[] GetUrlHash(string url)
        {
            return Crypto.Hash160(Utility.StrictUTF8.GetBytes(url));
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_RequestId), new StorageItem(BitConverter.GetBytes(0ul)));
        }

        protected override void PostPersist(ApplicationEngine engine)
        {
            base.PostPersist(engine);
            (UInt160 Account, BigInteger GAS)[] nodes = null;
            foreach (Transaction tx in engine.Snapshot.PersistingBlock.Transactions)
            {
                //Filter the response transactions
                OracleResponse response = tx.GetAttribute<OracleResponse>();
                if (response is null) continue;

                //Remove the request from storage
                StorageKey key = CreateStorageKey(Prefix_Request).Add(response.Id);
                OracleRequest request = engine.Snapshot.Storages[key].GetInteroperable<OracleRequest>();
                engine.Snapshot.Storages.Delete(key);

                //Remove the id from IdList
                key = CreateStorageKey(Prefix_IdList).Add(GetUrlHash(request.Url));
                IdList list = engine.Snapshot.Storages.GetAndChange(key).GetInteroperable<IdList>();
                if (!list.Remove(response.Id)) throw new InvalidOperationException();
                if (list.Count == 0) engine.Snapshot.Storages.Delete(key);

                //Mint GAS for oracle nodes
                nodes ??= NativeContract.Designate.GetDesignatedByRole(engine.Snapshot, Role.Oracle, engine.Snapshot.PersistingBlock.Index).Select(p => (Contract.CreateSignatureRedeemScript(p).ToScriptHash(), BigInteger.Zero)).ToArray();
                if (nodes.Length > 0)
                {
                    int index = (int)(response.Id % (ulong)nodes.Length);
                    nodes[index].GAS += OracleRequestPrice;
                }
            }
            if (nodes != null)
            {
                foreach (var (account, gas) in nodes)
                {
                    if (gas.Sign > 0) GAS.Mint(engine, account, gas);
                }
            }
        }

        [ContractMethod(OracleRequestPrice, CallFlags.AllowModifyStates)]
        private void Request(ApplicationEngine engine, string url, string filter, string callback, StackItem userData, long gasForResponse)
        {
            //Check the arguments
            if (Utility.StrictUTF8.GetByteCount(url) > MaxUrlLength
                || (filter != null && Utility.StrictUTF8.GetByteCount(filter) > MaxFilterLength)
                || Utility.StrictUTF8.GetByteCount(callback) > MaxCallbackLength
                || gasForResponse < 0_00333333)
                throw new ArgumentException();

            //Mint gas for the response
            engine.AddGas(gasForResponse);
            GAS.Mint(engine, Hash, gasForResponse);

            //Increase the request id
            StorageItem item_id = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RequestId));
            ulong id = BitConverter.ToUInt64(item_id.Value) + 1;
            item_id.Value = BitConverter.GetBytes(id);

            //Put the request to storage
            if (engine.Snapshot.Contracts.TryGet(engine.CallingScriptHash) is null)
                throw new InvalidOperationException();
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Request).Add(item_id.Value), new StorageItem(new OracleRequest
            {
                OriginalTxid = GetOriginalTxid(engine),
                GasForResponse = gasForResponse,
                Url = url,
                Filter = filter,
                CallbackContract = engine.CallingScriptHash,
                CallbackMethod = callback,
                UserData = BinarySerializer.Serialize(userData, MaxUserDataLength)
            }));

            //Add the id to the IdList
            var list = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_IdList).Add(GetUrlHash(url)), () => new StorageItem(new IdList())).GetInteroperable<IdList>();
            if (list.Count >= 256)
                throw new InvalidOperationException("There are too many pending responses for this url");
            list.Add(id);
        }

        [ContractMethod(0_00033333, CallFlags.None)]
        private bool Verify(ApplicationEngine engine)
        {
            Transaction tx = (Transaction)engine.ScriptContainer;
            return tx?.GetAttribute<OracleResponse>() != null;
        }

        private class IdList : List<ulong>, IInteroperable
        {
            public void FromStackItem(StackItem stackItem)
            {
                foreach (StackItem item in (VM.Types.Array)stackItem)
                    Add((ulong)item.GetInteger());
            }

            public StackItem ToStackItem(ReferenceCounter referenceCounter)
            {
                return new VM.Types.Array(referenceCounter, this.Select(p => (Integer)p));
            }
        }
    }
}
