using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native.Tokens;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    public class OracleContract : NativeContract
    {
        public override string Name => "Oracle";
        public override int Id => -5;

        private const byte Prefix_Validator = 37;
        private const byte Prefix_RequestBaseFee = 13;
        private const byte Prefix_RequestMaxValidHeight = 33;
        private const byte Prefix_Request = 21;
        private const byte Prefix_Response = 27;

        private const long ResponseTxMinFee = 1000;
        private string[] SupportedProtocol = new string[] { "http", "https" };

        public OracleContract()
        {
            Manifest.Features = ContractFeatures.HasStorage | ContractFeatures.Payable;
            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractEventDescriptor()
                {
                    Name = "Request",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "request",
                            Type = ContractParameterType.InteropInterface
                        }
                    }
                }
            };
            Manifest.Abi.Events = events.ToArray();
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public bool Verify(ApplicationEngine engine)
        {
            UInt160 oracleAddress = GetOracleMultiSigAddress(engine.Snapshot);
            return engine.CheckWitnessInternal(oracleAddress);
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public bool SetOracleValidators(ApplicationEngine engine, byte[] data)
        {
            ECPoint[] validators = data.AsSerializableArray<ECPoint>();
            UInt160 committeeAddress = NEO.GetCommitteeAddress(engine.Snapshot);
            if (validators.Length == 0 || !engine.CheckWitnessInternal(committeeAddress)) return false;
            var storageItem = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Validator), () => new StorageItem());
            storageItem.Value = validators.ToByteArray();
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public ECPoint[] GetOracleValidators(StoreView snapshot)
        {
            StorageKey key = CreateStorageKey(Prefix_Validator);
            StorageItem item = snapshot.Storages.TryGet(key);
            return item?.Value.AsSerializableArray<ECPoint>();
        }

        public UInt160 GetOracleMultiSigAddress(StoreView snapshot)
        {
            ECPoint[] oracleValidators = GetOracleValidators(snapshot);
            return Contract.CreateMultiSigContract(oracleValidators.Length - (oracleValidators.Length - 1) / 3, oracleValidators).ScriptHash;
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        public bool SetRequestBaseFee(ApplicationEngine engine, long requestBaseFee)
        {
            UInt160 account = NEO.GetCommitteeAddress(engine.Snapshot);
            if (!engine.CheckWitnessInternal(account)) return false;
            if (requestBaseFee <= 0) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RequestBaseFee), () => new StorageItem());
            storage.Value = BitConverter.GetBytes(requestBaseFee);
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public long GetRequestBaseFee(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_RequestBaseFee));
            if (storage is null) return 0;
            return BitConverter.ToInt64(storage.Value);
        }

        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        public bool SetRequestMaxValidHeight(ApplicationEngine engine, uint ValidHeight)
        {
            UInt160 committeeAddress = NEO.GetCommitteeAddress(engine.Snapshot);
            if (!engine.CheckWitnessInternal(committeeAddress)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_RequestMaxValidHeight), () => new StorageItem());
            storage.Value = BitConverter.GetBytes(ValidHeight);
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public uint GetRequestMaxValidHeight(StoreView snapshot)
        {
            StorageItem storage = snapshot.Storages.TryGet(CreateStorageKey(Prefix_RequestMaxValidHeight));
            if (storage is null) return 0;
            return BitConverter.ToUInt32(storage.Value);
        }

        [ContractMethod(0_01000000, CallFlags.All)]
        public bool Request(ApplicationEngine engine, string url, string filterPath, string callbackMethod, long oracleFee)
        {
            Transaction tx = (Transaction)engine.GetScriptContainer();
            var requestKey = CreateRequestKey(tx.Hash);
            if (engine.Snapshot.Storages.TryGet(requestKey) != null) throw new ArgumentException("One transaction can only request once");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) throw new ArgumentException("It's not a valid request");
            if (!SupportedProtocol.Contains(uri.Scheme.ToLowerInvariant())) throw new ArgumentException($"The scheme '{uri.Scheme}' is not allowed");
            if (oracleFee < GetRequestBaseFee(engine.Snapshot) + ResponseTxMinFee) throw new InvalidOperationException("OracleFee is not enough");

            // OracleFee = RequestBaseFee + FilterCost + ResponseTxFee
            // FilterCost = Size of the requested data * GasPerByte
            // ResponseTxFee = ResponseTx.NetwrokFee + ResponseTx.SystemFee

            engine.AddGas(oracleFee);
            GAS.Mint(engine, Hash, oracleFee - GetRequestBaseFee(engine.Snapshot)); // pay response tx

            OracleRequest request = new OracleRequest()
            {
                Url = url,
                FilterPath = filterPath,
                CallbackContract = engine.CallingScriptHash,
                CallbackMethod = callbackMethod,
                OracleFee = oracleFee,
                RequestTxHash = tx.Hash,
                ValidHeight = engine.GetBlockchainHeight() + GetRequestMaxValidHeight(engine.Snapshot),
                Status = RequestStatusType.Request
            };
            engine.Snapshot.Storages.Add(requestKey, new StorageItem(request));
            engine.SendNotification(Hash, "Request", new Array() { StackItem.FromInterface(request) });
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public OracleRequest GetRequest(StoreView snapshot, UInt256 requestTxHash)
        {
            return snapshot.Storages.TryGet(CreateRequestKey(requestTxHash))?.GetInteroperable<OracleRequest>();
        }

        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public OracleResponseAttribute GetResponse(ApplicationEngine engine, UInt256 requestTxHash)
        {
            var item = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_Response, requestTxHash));
            if (item is null || item.Value is null) throw new ArgumentException("Response dose not exist");
            var responseTxHash = new UInt256(item.Value);
            return engine.Snapshot.Transactions.TryGet(responseTxHash).Transaction.Attributes.OfType<OracleResponseAttribute>().First();
        }

        private bool Response(ApplicationEngine engine, UInt256 responseTxHash, OracleResponseAttribute response)
        {
            OracleRequest request = engine.Snapshot.Storages.TryGet(CreateRequestKey(response.RequestTxHash))?.GetInteroperable<OracleRequest>();
            if (request is null || request.Status != RequestStatusType.Request || request.ValidHeight < engine.Snapshot.Height) return false;
            request.Status = RequestStatusType.Ready;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Response, response.RequestTxHash), new StorageItem() { Value = responseTxHash.ToArray() });
            return true;
        }

        [ContractMethod(0_01000000, CallFlags.All)]
        public void Callback(ApplicationEngine engine)
        {
            UInt160 oracleAddress = GetOracleMultiSigAddress(engine.Snapshot);
            if (!engine.CheckWitnessInternal(oracleAddress)) throw new InvalidOperationException();
            Transaction tx = (Transaction)engine.ScriptContainer;
            if (tx is null) throw new InvalidOperationException();
            OracleResponseAttribute response = tx.Attributes.OfType<OracleResponseAttribute>().FirstOrDefault();
            if (response is null) throw new InvalidOperationException();
            StorageKey requestKey = CreateRequestKey(response.RequestTxHash);
            OracleRequest request = engine.Snapshot.Storages.GetAndChange(requestKey)?.GetInteroperable<OracleRequest>();
            if (request is null || request.Status != RequestStatusType.Ready) throw new InvalidOperationException();

            engine.CallFromNativeContract(() =>
            {
                request.Status = RequestStatusType.Successed;
            }, request.CallbackContract, request.CallbackMethod, response.Data);
        }

        protected override void OnPersist(ApplicationEngine engine)
        {
            base.OnPersist(engine);
            foreach (Transaction tx in engine.Snapshot.PersistingBlock.Transactions)
            {
                OracleResponseAttribute response = tx.Attributes.OfType<OracleResponseAttribute>().FirstOrDefault();
                if (response is null) continue;
                if (Response(engine, tx.Hash, response))
                {
                    UInt160[] oracleNodes = GetOracleValidators(engine.Snapshot).Select(p => Contract.CreateSignatureContract(p).ScriptHash).ToArray();
                    long nodeReward = (response.FilterCost + GetRequestBaseFee(engine.Snapshot)) / oracleNodes.Length;
                    foreach (UInt160 account in oracleNodes)
                        GAS.Mint(engine, account, nodeReward);

                    OracleRequest request = engine.Snapshot.Storages.TryGet(CreateRequestKey(response.RequestTxHash))?.GetInteroperable<OracleRequest>();
                    long refund = request.OracleFee - response.FilterCost - GetRequestBaseFee(engine.Snapshot) - tx.NetworkFee - tx.SystemFee;
                    Transaction requestTx = engine.Snapshot.Transactions.TryGet(request.RequestTxHash).Transaction;
                    GAS.Mint(engine, requestTx.Sender, refund);
                    GAS.Burn(engine, Hash, refund + response.FilterCost);
                }
            }
        }

        private StorageKey CreateRequestKey(UInt256 requestTxHash)
        {
            return CreateStorageKey(Prefix_Request, requestTxHash.ToArray());
        }
    }
}
