using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.Oracle
{
    public sealed class OraclePolicyContract : NativeContract
    {
        public override string ServiceName => "Neo.Native.Oracle.Policy";

        public override int Id => -4;

        private const byte Prefix_Validator = 24;
        private const byte Prefix_HttpConfig = 25;
        private const byte Prefix_PerRequestFee = 26;

        /// <summary>
        /// Constructor
        /// </summary>
        public OraclePolicyContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        /// <summary>
        /// Initialization.Set default parameter value.
        /// </summary>
        /// <param name="engine">VM</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_HttpConfig), new StorageItem
            {
                Value = new OracleHttpConfig() { Timeout = 5000 }.ToArray()
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_PerRequestFee), new StorageItem
            {
                Value = BitConverter.GetBytes(1000)
            });
            return true;
        }

        /// <summary>
        /// Oracle validator can delegate third party to operate Oracle nodes
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.ByteArray }, ParameterNames = new[] { "consignorPubKey", "consigneePubKey" })]
        private StackItem DelegateOracleValidator(ApplicationEngine engine, Array args)
        {
            ECPoint consignorPubKey = args[0].GetSpan().AsSerializable<ECPoint>();
            ECPoint consigneePubKey = args[1].GetSpan().AsSerializable<ECPoint>();
            UInt160 account = Contract.CreateSignatureRedeemScript(consignorPubKey).ToScriptHash();
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            StoreView snapshot = engine.Snapshot;
            StorageKey key = CreateStorageKey(Prefix_Validator, consignorPubKey);
            if (snapshot.Storages.TryGet(key) != null)
            {
                StorageItem value = snapshot.Storages.GetAndChange(key);
                value.Value = consigneePubKey.ToArray();
            }
            else
            {
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = consigneePubKey.ToArray()
                });
            }

            byte[] prefixKey = StorageKey.CreateSearchPrefix(Id, new[] { Prefix_Validator });
            List<ECPoint> delegatedOracleValidators = snapshot.Storages.Find(prefixKey).Select(p =>
              (
                  p.Key.Key.AsSerializable<ECPoint>()
              )).ToList();
            ECPoint[] oraclePubKeys = PolicyContract.NEO.GetValidators(snapshot);
            foreach (var item in delegatedOracleValidators)
            {
                if (!oraclePubKeys.Contains(item))
                {
                    snapshot.Storages.Delete(CreateStorageKey(Prefix_Validator, item));
                }
            }
            return true;
        }

        /// <summary>
        /// Get current authorized Oracle validator.
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Authorized Oracle validator</returns>
        [ContractMethod(0_01000000, ContractParameterType.Array)]
        private StackItem GetOracleValidators(ApplicationEngine engine, Array args)
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
            ECPoint[] oraclePubKeys = PolicyContract.NEO.GetValidators(snapshot);
            for (int index = 0; index < oraclePubKeys.Length; index++)
            {
                var oraclePubKey = oraclePubKeys[index];
                StorageKey key = CreateStorageKey(Prefix_Validator, oraclePubKey);
                ECPoint delegatePubKey = snapshot.Storages.TryGet(key)?.Value.AsSerializable<ECPoint>();
                if (delegatePubKey != null) { oraclePubKeys[index] = delegatePubKey; }
            }
            return oraclePubKeys;
        }

        /// <summary>
        /// Get number of current authorized Oracle validator
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>The number of authorized Oracle validator</returns>
        [ContractMethod(0_01000000, ContractParameterType.Integer)]
        private StackItem GetOracleValidatorsCount(ApplicationEngine engine, Array args)
        {
            return GetOracleValidatorsCount(engine.Snapshot);
        }

        /// <summary>
        /// Get number of current authorized Oracle validator
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>The number of authorized Oracle validator</returns>
        public BigInteger GetOracleValidatorsCount(StoreView snapshot)
        {
            return GetOracleValidators(snapshot).Length;
        }

        /// <summary>
        /// Create a Oracle multisignature address
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>Oracle multisignature address</returns>
        public UInt160 GetOracleMultiSigAddress(StoreView snapshot)
        {
            ECPoint[] validators = GetOracleValidators(snapshot);
            return Contract.CreateMultiSigRedeemScript(validators.Length - (validators.Length - 1) / 3, validators).ToScriptHash();
        }

        /// <summary>
        /// Set HttpConfig
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "httpConfig" })]
        private StackItem SetHttpConfig(ApplicationEngine engine, Array args)
        {
            StoreView snapshot = engine.Snapshot;
            UInt160 account = GetOracleMultiSigAddress(snapshot);
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            int timeOutMilliSeconds = (int)args[0].GetBigInteger();
            if (timeOutMilliSeconds <= 0) return false;
            OracleHttpConfig httpConfig = new OracleHttpConfig() { Timeout = timeOutMilliSeconds };
            StorageItem storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_HttpConfig));
            storage.Value = httpConfig.ToArray();
            return true;
        }

        /// <summary>
        /// Get HttpConfig
        /// </summary>
        /// <param name="engine">VM</param>
        /// <returns>value</returns>
        [ContractMethod(0_01000000, ContractParameterType.Array)]
        private StackItem GetHttpConfig(ApplicationEngine engine, Array args)
        {
            return GetHttpConfig(engine.Snapshot).ToStackItem(engine.ReferenceCounter);
        }

        /// <summary>
        /// Get HttpConfig
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>value</returns>
        public OracleHttpConfig GetHttpConfig(StoreView snapshot)
        {
            return snapshot.Storages[CreateStorageKey(Prefix_HttpConfig)].Value.AsSerializable<OracleHttpConfig>();
        }

        /// <summary>
        /// Set PerRequestFee
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "fee" })]
        private StackItem SetPerRequestFee(ApplicationEngine engine, Array args)
        {
            StoreView snapshot = engine.Snapshot;
            UInt160 account = GetOracleMultiSigAddress(snapshot);
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            int perRequestFee = (int)args[0].GetBigInteger();
            if (perRequestFee <= 0) return false;
            StorageItem storage = snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_PerRequestFee));
            storage.Value = BitConverter.GetBytes(perRequestFee);
            return true;
        }

        /// <summary>
        /// Get PerRequestFee
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Value</returns>
        [ContractMethod(0_01000000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetPerRequestFee(ApplicationEngine engine, Array args)
        {
            return new Integer(GetPerRequestFee(engine.Snapshot));
        }

        /// <summary>
        /// Get PerRequestFee
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>Value</returns>
        public int GetPerRequestFee(StoreView snapshot)
        {
            return BitConverter.ToInt32(snapshot.Storages[CreateStorageKey(Prefix_PerRequestFee)].Value, 0);
        }
    }
}
