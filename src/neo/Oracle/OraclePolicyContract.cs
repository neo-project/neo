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

        public OraclePolicyContract()
        {
            Manifest.Features = ContractFeatures.HasStorage;
        }

        internal override bool Initialize(ApplicationEngine engine)
        {
            if (!base.Initialize(engine)) return false;
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_TimeOutMilliSeconds), new StorageItem
            {
                Value = BitConverter.GetBytes(1000)
            });
            engine.Snapshot.Storages.Add(CreateStorageKey(Prefix_PerRequestFee), new StorageItem
            {
                Value = BitConverter.GetBytes(1000)
            });
            return true;
        }

        [ContractMethod(0_01000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Array }, ParameterNames = new[] { "account", "pubkeys" })]
        private StackItem DelegateOracleValidator(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            ECPoint[] pubkeys = ((Array)args[1]).Select(p => p.GetSpan().AsSerializable<ECPoint>()).ToArray();
            if (pubkeys.Length != 2) return false;
            StoreView snapshot = engine.Snapshot;
            StorageKey key = CreateStorageKey(Prefix_Validator, pubkeys[0]);
            if (snapshot.Storages.TryGet(key) != null)
            {
                StorageItem value = snapshot.Storages.GetAndChange(key);
                value.Value = pubkeys[1].ToArray();
            }
            else
            {
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = pubkeys[1].ToArray()
                });
            }
            return true;
        }

        [ContractMethod(0_03000000, ContractParameterType.Array)]
        private StackItem GetOracleValidators(ApplicationEngine engine, Array args)
        {
            return new Array(engine.ReferenceCounter, GetOracleValidators(engine.Snapshot).Select(p => (StackItem)p.ToArray()));
        }

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

        [ContractMethod(0_03000000, ContractParameterType.Integer)]
        private StackItem GetOracleValidatorsCount(ApplicationEngine engine, Array args)
        {
            return GetOracleValidatorsCount(engine.Snapshot);
        }

        public BigInteger GetOracleValidatorsCount(StoreView snapshot)
        {
            return GetOracleValidators(snapshot).Length;
        }

        internal IEnumerable<(ECPoint ConsensusPublicKey, ECPoint OraclePublicKey)> GetDelegateOracleValidators(StoreView snapshot)
        {
            byte[] prefix_key = StorageKey.CreateSearchPrefix(Id, new[] { Prefix_Validator });
            return snapshot.Storages.Find(prefix_key).Select(p =>
            (
                p.Key.Key.AsSerializable<ECPoint>(1),
                p.Value.Value.AsSerializable<ECPoint>(1)
            ));
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "fee" })]
        private StackItem SetTimeOutMilliSeconds(ApplicationEngine engine, Array args)
        {
            StoreView snapshot = engine.Snapshot;
            Transaction tx = (Transaction)engine.ScriptContainer;
            Array signatures = new Array(tx.Witnesses.ToList().Select(p => StackItem.FromInterface(p.VerificationScript)));
            Array pubkeys = new Array(GetOracleValidators(snapshot).ToList().Select(p => StackItem.FromInterface(p)));
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            engine.CurrentContext.EvaluationStack.Push(StackItem.Null);

            if (BitConverter.ToInt32(args[0].GetSpan()) <= 0) return false;
            int timeOutMilliSeconds = BitConverter.ToInt32(args[0].GetSpan());
            if (!InteropService.Crypto.ECDsaCheckMultiSig.Handler.Invoke(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_TimeOutMilliSeconds));
            storage.Value = BitConverter.GetBytes(timeOutMilliSeconds);
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Integer)]
        private StackItem GetTimeOutMilliSeconds(ApplicationEngine engine, Array args)
        {
            return new Integer(GetTimeOutMilliSeconds(engine.Snapshot));
        }

        public int GetTimeOutMilliSeconds(StoreView snapshot)
        {
            return BitConverter.ToInt32(snapshot.Storages[CreateStorageKey(Prefix_TimeOutMilliSeconds)].Value, 0);
        }

        [ContractMethod(0_03000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Integer }, ParameterNames = new[] { "fee" })]
        private StackItem SetPerRequestFee(ApplicationEngine engine, Array args)
        {
            StoreView snapshot = engine.Snapshot;
            Transaction tx = (Transaction)engine.ScriptContainer;
            Array signatures = new Array(tx.Witnesses.ToList().Select(p => StackItem.FromInterface(p.VerificationScript)));
            Array pubkeys = new Array(GetOracleValidators(snapshot).ToList().Select(p => StackItem.FromInterface(p)));
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            engine.CurrentContext.EvaluationStack.Push(StackItem.Null);
            if (BitConverter.ToInt32(args[0].GetSpan()) <= 0) return false;
            int perRequestFee = BitConverter.ToInt32(args[0].GetSpan());
            if (!InteropService.Crypto.ECDsaCheckMultiSig.Handler.Invoke(engine)) return false;
            StorageItem storage = engine.Snapshot.Storages.GetAndChange(CreateStorageKey(Prefix_PerRequestFee));
            storage.Value = BitConverter.GetBytes(perRequestFee);
            return true;
        }

        [ContractMethod(1_00000000, ContractParameterType.Integer, SafeMethod = true)]
        private StackItem GetPerRequestFee(ApplicationEngine engine, Array args)
        {
            return new Integer(GetPerRequestFee(engine.Snapshot));
        }

        public int GetPerRequestFee(StoreView snapshot)
        {
            return BitConverter.ToInt32(snapshot.Storages[CreateStorageKey(Prefix_PerRequestFee)].Value, 0);
        }
    }
}
