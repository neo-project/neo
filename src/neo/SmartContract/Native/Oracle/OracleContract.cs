#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.SmartContract.Native.Oracle
{
    public sealed partial class OracleContract : NativeContract
    {
        private const int MaxUrlLength = 256;
        private const int MaxCallbackLength = 32;
        private const int MaxUserDataLength = 128;

        private const byte Prefix_NodeList = 8;
        private const byte Prefix_RequestId = 9;
        private const byte Prefix_Request = 7;
        private const byte Prefix_IdList = 6;

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
            OracleResponse response = tx.Attributes.OfType<OracleResponse>().First();
            StorageKey key = CreateStorageKey(Prefix_Request, BitConverter.GetBytes(response.Id));
            OracleRequest request = engine.Snapshot.Storages[key].GetInteroperable<OracleRequest>();
            engine.Snapshot.Storages.Delete(key);
            key = CreateStorageKey(Prefix_IdList, GetUrlHash(request.Url));
            IdList list = engine.Snapshot.Storages.GetAndChange(key).GetInteroperable<IdList>();
            if (!list.Remove(response.Id)) throw new InvalidOperationException();
            if (list.Count == 0) engine.Snapshot.Storages.Delete(key);
            StackItem userData = BinarySerializer.Deserialize(request.UserData, engine.MaxStackSize, engine.MaxItemSize, engine.ReferenceCounter);
            engine.CallFromNativeContract(null, request.CallbackContract, request.CallbackMethod, request.Url, userData, response.Success, response.Result);
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetOracleNodes(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_NodeList)].GetInteroperable<NodeList>().ToArray();
        }

        public OracleRequest GetRequest(StoreView snapshot, ulong id)
        {
            return snapshot.Storages.TryGet(CreateStorageKey(Prefix_Request, BitConverter.GetBytes(id)))?.GetInteroperable<OracleRequest>();
        }

        public IEnumerable<OracleRequest> GetRequests(StoreView snapshot)
        {
            return snapshot.Storages.Find(new byte[] { Prefix_Request }).Select(p => p.Value.GetInteroperable<OracleRequest>());
        }

        public IEnumerable<OracleRequest> GetRequestsByUrl(StoreView snapshot, string url)
        {
            IdList list = snapshot.Storages.TryGet(CreateStorageKey(Prefix_IdList, GetUrlHash(url)))?.GetInteroperable<IdList>();
            if (list is null) yield break;
            foreach (ulong id in list)
                yield return snapshot.Storages[CreateStorageKey(Prefix_Request, BitConverter.GetBytes(id))].GetInteroperable<OracleRequest>();
        }

        private static byte[] GetUrlHash(string url)
        {
            return Crypto.Hash160(Utility.StrictUTF8.GetBytes(url));
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_NodeList), new StorageItem(new NodeList()));
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_RequestId), new StorageItem(BitConverter.GetBytes(0ul)));
        }

        [ContractMethod(0_50000000, CallFlags.AllowModifyStates)]
        private void Request(ApplicationEngine engine, string url, string callback, StackItem userData)
        {
            //TODO: Add filter
            if (Utility.StrictUTF8.GetByteCount(url) > MaxUrlLength || Utility.StrictUTF8.GetByteCount(callback) > MaxCallbackLength)
                throw new ArgumentException();
            StorageItem item_id = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RequestId));
            ulong id = BitConverter.ToUInt64(item_id.Value) + 1;
            item_id.Value = BitConverter.GetBytes(id);
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Request, item_id.Value), new StorageItem(new OracleRequest
            {
                Url = url,
                Txid = ((Transaction)engine.ScriptContainer).Hash,
                CallbackContract = engine.CallingScriptHash,
                CallbackMethod = callback,
                UserData = BinarySerializer.Serialize(userData, MaxUserDataLength)
            }));
            engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_IdList, GetUrlHash(url)), () => new StorageItem(new IdList())).GetInteroperable<IdList>().Add(id);
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        private void SetOracleNodes(ApplicationEngine engine, ECPoint[] nodes)
        {
            //TODO: How to incentivize oracle nodes?
            if (!CheckCommittees(engine)) throw new InvalidOperationException();
            NodeList list = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_NodeList)).GetInteroperable<NodeList>();
            list.Clear();
            list.AddRange(nodes);
        }
    }
}
