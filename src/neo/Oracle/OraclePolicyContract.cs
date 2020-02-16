using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
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
        private const byte Prefix_TimeOutMilliSeconds = 25;
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
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_TimeOutMilliSeconds), new StorageItem
            {
                Value = BitConverter.GetBytes(5000)
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
        [ContractMethod(0_01000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.ByteArray, ContractParameterType.ByteArray }, ParameterNames = new[] { "consignorPubKey", "consigneePubKey" })]
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
            return true;
        }

        /// <summary>
        /// Get current authorized Oracle validator.
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Authorized Oracle validator</returns>
        [ContractMethod(0_03000000, ContractParameterType.Array)]
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
            ECPoint[] consensusPublicKey = PolicyContract.NEO.GetValidators(snapshot);
            IEnumerable<(ECPoint ConsensusPublicKey, ECPoint OraclePublicKey)> hasDelegateOracleValidators = GetDelegateOracleValidators(snapshot).Where(p => consensusPublicKey.Contains(p.ConsensusPublicKey));
            foreach (var item in hasDelegateOracleValidators)
            {
                var index = System.Array.IndexOf(consensusPublicKey, item.ConsensusPublicKey);
                if (index >= 0) consensusPublicKey[index] = item.OraclePublicKey;
            }
            return consensusPublicKey;
        }

        /// <summary>
        /// Get number of current authorized Oracle validator
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>The number of authorized Oracle validator</returns>
        [ContractMethod(0_03000000, ContractParameterType.Integer)]
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
        /// A collection of delegated Oracle validator
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>delegated Oracle validator</returns>
        internal IEnumerable<(ECPoint ConsensusPublicKey, ECPoint OraclePublicKey)> GetDelegateOracleValidators(StoreView snapshot)
        {
            byte[] prefix_key = StorageKey.CreateSearchPrefix(Id, new[] { Prefix_Validator });
            return snapshot.Storages.Find(prefix_key).Select(p =>
            (
                p.Key.Key.AsSerializable<ECPoint>(1),
                p.Value.Value.AsSerializable<ECPoint>(1)
            ));
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
        /// Set timeout
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Returns true if the execution is successful, otherwise returns false</returns>
        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "TimeOutMilliSeconds" })]
        private StackItem SetTimeOutMilliSeconds(ApplicationEngine engine, Array args)
        {
            StoreView snapshot = engine.Snapshot;
            UInt160 account = GetOracleMultiSigAddress(snapshot);
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            if (BitConverter.ToInt32(args[0].GetSpan()) <= 0) return false;
            int timeOutMilliSeconds = BitConverter.ToInt32(args[0].GetSpan());
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TimeOutMilliSeconds));
            storage.Value = BitConverter.GetBytes(timeOutMilliSeconds);
            return true;
        }

        /// <summary>
        /// Get timeout
        /// </summary>
        /// <param name="engine">VM</param>
        /// <returns>value</returns>
        [ContractMethod(1_00000000, ContractParameterType.Integer)]
        private StackItem GetTimeOutMilliSeconds(ApplicationEngine engine, Array args)
        {
            return new Integer(GetTimeOutMilliSeconds(engine.Snapshot));
        }

        /// <summary>
        /// Get timeout
        /// </summary>
        /// <param name="snapshot">snapshot</param>
        /// <returns>value</returns>
        public int GetTimeOutMilliSeconds(StoreView snapshot)
        {
            return BitConverter.ToInt32(snapshot.Storages[CreateStorageKey(Prefix_TimeOutMilliSeconds)].Value, 0);
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
            if (BitConverter.ToInt32(args[0].GetSpan()) <= 0) return false;
            int perRequestFee = BitConverter.ToInt32(args[0].GetSpan());
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_PerRequestFee));
            storage.Value = BitConverter.GetBytes(perRequestFee);
            return true;
        }

        /// <summary>
        /// Get PerRequestFee
        /// </summary>
        /// <param name="engine">VM</param>
        /// <param name="args">Parameter Array</param>
        /// <returns>Value</returns>
        [ContractMethod(1_00000000, ContractParameterType.Integer, SafeMethod = true)]
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
