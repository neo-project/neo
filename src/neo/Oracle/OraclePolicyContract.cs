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

        [ContractMethod(0_01000000, ContractParameterType.Boolean, ParameterTypes = new[] { ContractParameterType.Hash160, ContractParameterType.Array }, ParameterNames = new[] { "account", "pubkeys" })]
        private StackItem DelegateOracleValidator(ApplicationEngine engine, Array args)
        {
            UInt160 account = new UInt160(args[0].GetSpan());
            if (!InteropService.Runtime.CheckWitnessInternal(engine, account)) return false;
            ECPoint[] pubkeys = ((Array)args[1]).Select(p => p.GetSpan().AsSerializable<ECPoint>()).ToArray();
            if (pubkeys.Length != 2) return false;
            StoreView snapshot = engine.Snapshot;
            StorageKey key = CreateStorageKey(Prefix_Validator, pubkeys[0]);
            if (snapshot.Storages.TryGet(key) != null) {
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

        public ECPoint[] GetOracleValidators(StoreView snapshot)
        {
            ECPoint[] consensusPublicKey = PolicyContract.NEO.GetValidators(snapshot);
            IEnumerable<(ECPoint ConsensusPublicKey, ECPoint OraclePublicKey)> hasDelegateOracleValidators=GetDelegateOracleValidators(snapshot).Where(p => consensusPublicKey.Contains(p.ConsensusPublicKey));
            hasDelegateOracleValidators.ToList().ForEach(p=>{
                var index = System.Array.IndexOf(consensusPublicKey, p.ConsensusPublicKey);
                if (index >= 0) consensusPublicKey[index] = p.OraclePublicKey;
            });
            return consensusPublicKey;
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
            Array pubkeys = new Array(GetOracleValidators(snapshot).ToList().Select(p=> StackItem.FromInterface(p)));
            engine.CurrentContext.EvaluationStack.Push(signatures);
            engine.CurrentContext.EvaluationStack.Push(pubkeys);
            engine.CurrentContext.EvaluationStack.Push(StackItem.Null);

            if (BitConverter.ToInt32(args[0].GetSpan()) <= 0) return false;
            int timeOutMilliSeconds = BitConverter.ToInt32(args[0].GetSpan());
            if (!InteropService.Crypto.ECDsaCheckMultiSig.Handler.Invoke(engine))
            {
                return false;
            }
            StorageKey key = CreateStorageKey(Prefix_TimeOutMilliSeconds);
            if (snapshot.Storages.TryGet(key) != null)
            {
                StorageItem value = snapshot.Storages.GetAndChange(key);
                value.Value = BitConverter.GetBytes(timeOutMilliSeconds);
                return true;
            }
            else
            {
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = BitConverter.GetBytes(timeOutMilliSeconds)
                });
                return true;
            }
        }

        public int GetTimeOutMilliSeconds(StoreView snapshot)
        {
            StorageKey key = CreateStorageKey(Prefix_TimeOutMilliSeconds);
            if (snapshot.Storages.TryGet(key) != null) {
                return BitConverter.ToInt32(snapshot.Storages.TryGet(key).Value, 0);
            }
            else
            {
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = BitConverter.GetBytes(1000)
                });
                return 1000;
            }
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
            if (!InteropService.Crypto.ECDsaCheckMultiSig.Handler.Invoke(engine)) {
                return false;
            }
            StorageKey key = CreateStorageKey(Prefix_PerRequestFee);
            if (snapshot.Storages.TryGet(key) != null)
            {
                StorageItem value = snapshot.Storages.GetAndChange(key);
                value.Value = BitConverter.GetBytes(perRequestFee);
                return true;
            }
            else
            {
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = BitConverter.GetBytes(perRequestFee)
                });
                return true;
            }
        }

        public int GetPerRequestFee(SnapshotView snapshot)
        {
            StorageKey key = CreateStorageKey(Prefix_PerRequestFee);
            if (snapshot.Storages.TryGet(key) != null)
            {
                return BitConverter.ToInt32(snapshot.Storages.TryGet(key).Value, 0);
            }
            else
            {
                snapshot.Storages.Add(key, new StorageItem
                {
                    Value = BitConverter.GetBytes(1000)
                });
                return 1000;
            }
        }
    }
}
