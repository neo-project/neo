using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native.Oracle
{
    public sealed class OracleContract : NativeContract
    {
        public override string Name => "Oracle";

        public override int Id => -4;

        internal const byte Prefix_Validator = 24;
        internal const byte Prefix_Config = 25;
        internal const byte Prefix_PerRequestFee = 26;
        internal const byte Prefix_OracleResponse = 27;

        public OracleContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            base.Initialize(engine);
            if (GetPerRequestFee(engine) != 0) throw new ArgumentException("Already initialized");

            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_Config).Add(Encoding.UTF8.GetBytes(HttpConfig.Key)), new StorageItem(new HttpConfig(), false));
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_PerRequestFee), new StorageItem
            {
                Value = BitConverter.GetBytes(1000)
            });
        }

        /// <summary>
        /// Set Oracle Response Only
        /// </summary>
        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        private bool SetOracleResponse(ApplicationEngine engine, UInt256 txHash, byte[] response)
        {
            UInt160 account = GetOracleMultiSigAddress(engine.Snapshot);
            if (!engine.CheckWitnessInternal(account)) return false;

            // This only can be called by the oracle's multi signature

            var oracleResponse = response.AsSerializable<OracleExecutionCache>();

            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_OracleResponse).Add(txHash), () => new StorageItem());
            storage.Value = IO.Helper.ToArray(oracleResponse);
            return false;
        }

        /// <summary>
        /// Check if the response it's already stored
        /// </summary>
        /// <param name="snapshot">Snapshot</param>
        /// <param name="txHash">Transaction Hash</param>
        public bool ContainsResponse(StoreView snapshot, UInt256 txHash)
        {
            StorageKey key = CreateStorageKey(Prefix_OracleResponse).Add(txHash);
            return snapshot.Storages.TryGet(key) != null;
        }

        /// <summary>
        /// Consume Oracle Response
        /// </summary>
        /// <param name="engine">Engine</param>
        /// <param name="txHash">Transaction Hash</param>
        public OracleExecutionCache ConsumeOracleResponse(ApplicationEngine engine, UInt256 txHash)
        {
            StorageKey key = CreateStorageKey(Prefix_OracleResponse).Add(txHash);
            StorageItem storage = engine.Snapshot.Storages.TryGet(key);
            if (storage == null) return null;

            OracleExecutionCache ret = storage.Value.AsSerializable<OracleExecutionCache>();

            // It should be cached by the ApplicationEngine so we can save space removing it

            engine.Snapshot.Storages.Delete(key);

            // Pay for the filter

            engine.AddGas(ret.FilterCost);
            return ret;
        }

        /// <summary>
        /// Consensus node can delegate third party to operate Oracle nodes
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        public bool DelegateOracleValidator(ApplicationEngine engine, ECPoint consignorPubKey, ECPoint consigneePubKey)
        {
            StoreView snapshot = engine.Snapshot;
            ECPoint[] cnPubKeys = NEO.GetValidators(snapshot);
            if (!cnPubKeys.Contains(consignorPubKey)) return false;
            UInt160 account = Contract.CreateSignatureRedeemScript(consignorPubKey).ToScriptHash();
            if (!engine.CheckWitnessInternal(account)) return false;
            StorageKey key = CreateStorageKey(Prefix_Validator).Add(consignorPubKey);
            StorageItem item = snapshot.Storages.GetAndChange(key, () => new StorageItem());
            item.Value = consigneePubKey.ToArray();

            byte[] prefixKey = StorageKey.CreateSearchPrefix(Id, new[] { Prefix_Validator });
            List<ECPoint> delegatedOracleValidators = snapshot.Storages.Find(prefixKey).Select(p =>
              (
                  p.Key.Key.AsSerializable<ECPoint>(1)
              )).ToList();
            foreach (var validator in delegatedOracleValidators)
            {
                if (!cnPubKeys.Contains(validator))
                {
                    snapshot.Storages.Delete(CreateStorageKey(Prefix_Validator).Add(validator));
                }
            }
            return true;
        }

        /// <summary>
        /// Get current authorized Oracle validator.
        /// </summary>
        /// <param name="engine">VM</param>
        /// <returns>Authorized Oracle validator</returns>
        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        private StackItem GetOracleValidators(ApplicationEngine engine)
        {
            return new Array(engine.ReferenceCounter, GetOracleValidators(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

        /// <summary>
        /// Get current authorized Oracle validator
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>Authorized Oracle validator</returns>
        public ECPoint[] GetOracleValidators(StoreView snapshot)
        {
            ECPoint[] cnPubKeys = NEO.GetValidators(snapshot);
            ECPoint[] oraclePubKeys = new ECPoint[cnPubKeys.Length];
            System.Array.Copy(cnPubKeys, oraclePubKeys, cnPubKeys.Length);
            for (int index = 0; index < oraclePubKeys.Length; index++)
            {
                var oraclePubKey = oraclePubKeys[index];
                StorageKey key = CreateStorageKey(Prefix_Validator).Add(oraclePubKey);
                ECPoint delegatePubKey = snapshot.Storages.TryGet(key)?.Value.AsSerializable<ECPoint>();
                if (delegatePubKey != null) { oraclePubKeys[index] = delegatePubKey; }
            }
            return oraclePubKeys.Distinct().ToArray();
        }

        /// <summary>
        /// Get number of current authorized Oracle validator
        /// </summary>
        /// <param name="engine">VM</param>
        /// <returns>The number of authorized Oracle validator</returns>
        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public int GetOracleValidatorsCount(ApplicationEngine engine)
        {
            return GetOracleValidators(engine.Snapshot).Length;
        }

        /// <summary>
        /// Create a Oracle multisignature contract
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>Oracle multisignature address</returns>
        public Contract GetOracleMultiSigContract(StoreView snapshot)
        {
            ECPoint[] oracleValidators = GetOracleValidators(snapshot);
            return Contract.CreateMultiSigContract(oracleValidators.Length - (oracleValidators.Length - 1) / 3, oracleValidators);
        }

        /// <summary>
        /// Create a Oracle multisignature address
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>Oracle multisignature address</returns>
        public UInt160 GetOracleMultiSigAddress(StoreView snapshot)
        {
            return GetOracleMultiSigContract(snapshot).ScriptHash;
        }

        /// <summary>
        /// Set HttpConfig
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="type">Type</param>
        /// <param name="value">Value</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        public StackItem SetConfig(ApplicationEngine engine, string type, VM.Types.Array value)
        {
            StoreView snapshot = engine.Snapshot;
            UInt160 account = GetOracleMultiSigAddress(snapshot);
            if (!engine.CheckWitnessInternal(account)) return false;

            switch (type)
            {
                case HttpConfig.Key:
                    {
                        var newCfg = new HttpConfig();
                        newCfg.FromStackItem(value);

                        StorageItem storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Config).Add(Encoding.UTF8.GetBytes(HttpConfig.Key)));
                        var config = storage.GetInteroperable<HttpConfig>();
                        config.TimeOut = newCfg.TimeOut;
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Get HttpConfig
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="type">Typed</param>
        /// <returns>value</returns>
        [ContractMethod(0_01000000, CallFlags.AllowStates)]
        public object GetConfig(ApplicationEngine engine, string type)
        {
            switch (type)
            {
                case HttpConfig.Key:
                    {
                        return GetHttpConfig(engine.Snapshot).ToStackItem(engine.ReferenceCounter);
                    }
            }

            return false;
        }

        /// <summary>
        /// Get HttpConfig
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>value</returns>
        public HttpConfig GetHttpConfig(StoreView snapshot)
        {
            var storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_Config).Add(Encoding.UTF8.GetBytes(HttpConfig.Key)));
            return storage.GetInteroperable<HttpConfig>();
        }

        /// <summary>
        /// Set PerRequestFee
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="perRequestFee">Per Request fee</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, CallFlags.AllowModifyStates)]
        public bool SetPerRequestFee(ApplicationEngine engine, int perRequestFee)
        {
            StoreView snapshot = engine.Snapshot;
            UInt160 account = GetOracleMultiSigAddress(snapshot);
            if (!engine.CheckWitnessInternal(account)) return false;
            if (perRequestFee <= 0) return false;
            StorageItem storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_PerRequestFee));
            storage.Value = BitConverter.GetBytes(perRequestFee);
            return true;
        }

        /// <summary>
        /// Get PerRequestFee
        /// </summary>
        /// <param name="engine">VM</param>
        /// <returns>Value</returns>
        [ContractMethod(0_01000000, requiredCallFlags: CallFlags.AllowStates)]
        public int GetPerRequestFee(ApplicationEngine engine)
        {
            StorageItem storage = engine.Snapshot.Storages.TryGet(CreateStorageKey(Prefix_PerRequestFee));
            if (storage is null) return 0;
            return BitConverter.ToInt32(storage.Value);
        }

        /// <summary>
        /// Oracle get the hash of the current OracleFlow [Request/Response]
        /// </summary>
        [ContractMethod(0_01000000, requiredCallFlags: CallFlags.None)]
        public UInt160 GetResponseHash(ApplicationEngine engine)
        {
            if (engine.OracleCache == null)
            {
                return null;
            }
            else
            {
                return engine.OracleCache.Hash;
            }
        }

        /// <summary>
        /// Oracle Get
        ///     string url, [UInt160 filter], [string filterMethod], [string filterArgs]
        /// </summary>
        [ContractMethod(0_01000000, CallFlags.AllowModifyStates)]
        public byte[] Get(ApplicationEngine engine, string url, byte[] filterContract, string filterMethod, string filterArgs)
        {
            if (engine.OracleCache == null)
            {
                // We should enter here only during OnPersist with the OracleRequestTx

                if (engine.ScriptContainer is Transaction tx)
                {
                    // Read Oracle Response

                    engine.OracleCache = Oracle.ConsumeOracleResponse(engine, tx.Hash);

                    // If it doesn't exist, fault

                    if (engine.OracleCache == null)
                    {
                        throw new ArgumentException();
                    }
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) throw new ArgumentException();

            // Create filter

            OracleFilter filter;

            if (filterContract != null)
            {
                filter = new OracleFilter()
                {
                    ContractHash = new UInt160(filterContract),
                    FilterMethod = filterMethod,
                    FilterArgs = filterArgs ?? ""
                };
            }
            else
            {
                if (!string.IsNullOrEmpty(filterMethod))
                {
                    throw new ArgumentException("If the filter it's defined, the values can't be null");
                }

                filter = null;
            }

            // Create request

            OracleRequest request;
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "https":
                    {
                        request = new OracleHttpsRequest()
                        {
                            Method = HttpMethod.GET,
                            URL = uri,
                            Filter = filter
                        };
                        break;
                    }
                default: throw new ArgumentException($"The scheme '{uri.Scheme}' is not allowed");
            }

            // Execute the oracle request

            if (engine.OracleCache.TryGet(request, out var response))
            {
                // Add the gas filter cost

                return response.Result;
            }

            throw new ArgumentException();
        }
    }
}
