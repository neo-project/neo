using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public static class Helper
    {
        private const long MaxVerificationGas = 0_50000000;

        public static UInt160 GetScriptHash(this ExecutionContext context)
        {
            return context.GetState<ExecutionContextState>().ScriptHash;
        }

        public static bool IsMultiSigContract(this byte[] script)
        {
            return IsMultiSigContract(script, out _, out _, null);
        }

        public static bool IsMultiSigContract(this byte[] script, out int m, out int n)
        {
            return IsMultiSigContract(script, out m, out n, null);
        }

        public static bool IsMultiSigContract(this byte[] script, out int m, out ECPoint[] points)
        {
            List<ECPoint> list = new List<ECPoint>();
            if (IsMultiSigContract(script, out m, out _, list))
            {
                points = list.ToArray();
                return true;
            }
            else
            {
                points = null;
                return false;
            }
        }

        private static bool IsMultiSigContract(byte[] script, out int m, out int n, List<ECPoint> points)
        {
            m = 0; n = 0;
            int i = 0;
            if (script.Length < 43) return false;
            switch (script[i])
            {
                case (byte)OpCode.PUSHINT8:
                    m = script[++i];
                    ++i;
                    break;
                case (byte)OpCode.PUSHINT16:
                    m = BinaryPrimitives.ReadUInt16LittleEndian(script.AsSpan(++i));
                    i += 2;
                    break;
                case byte b when b >= (byte)OpCode.PUSH1 && b <= (byte)OpCode.PUSH16:
                    m = b - (byte)OpCode.PUSH0;
                    ++i;
                    break;
                default:
                    return false;
            }
            if (m < 1 || m > 1024) return false;
            while (script[i] == (byte)OpCode.PUSHDATA1)
            {
                if (script.Length <= i + 35) return false;
                if (script[++i] != 33) return false;
                points?.Add(ECPoint.DecodePoint(script.AsSpan(i + 1, 33), ECCurve.Secp256r1));
                i += 34;
                ++n;
            }
            if (n < m || n > 1024) return false;
            switch (script[i])
            {
                case (byte)OpCode.PUSHINT8:
                    if (script.Length <= i + 1 || n != script[++i]) return false;
                    ++i;
                    break;
                case (byte)OpCode.PUSHINT16:
                    if (script.Length < i + 3 || n != BinaryPrimitives.ReadUInt16LittleEndian(script.AsSpan(++i))) return false;
                    i += 2;
                    break;
                case byte b when b >= (byte)OpCode.PUSH1 && b <= (byte)OpCode.PUSH16:
                    if (n != b - (byte)OpCode.PUSH0) return false;
                    ++i;
                    break;
                default:
                    return false;
            }
            if (script.Length != i + 6) return false;
            if (script[i++] != (byte)OpCode.PUSHNULL) return false;
            if (script[i++] != (byte)OpCode.SYSCALL) return false;
            if (BitConverter.ToUInt32(script, i) != ApplicationEngine.Neo_Crypto_CheckMultisigWithECDsaSecp256r1)
                return false;
            return true;
        }

        public static bool IsSignatureContract(this byte[] script)
        {
            if (script.Length != 41) return false;
            if (script[0] != (byte)OpCode.PUSHDATA1
                || script[1] != 33
                || script[35] != (byte)OpCode.PUSHNULL
                || script[36] != (byte)OpCode.SYSCALL
                || BitConverter.ToUInt32(script, 37) != ApplicationEngine.Neo_Crypto_VerifyWithECDsaSecp256r1)
                return false;
            return true;
        }

        public static bool IsStandardContract(this byte[] script)
        {
            return script.IsSignatureContract() || script.IsMultiSigContract();
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Hash160(script));
        }

        public static UInt160 ToScriptHash(this ReadOnlySpan<byte> script)
        {
            return new UInt160(Crypto.Hash160(script));
        }

        public static bool TryCreateVerifyEngine(this IVerifiable verifiable, StoreView snapshot, UInt160 hash, Witness witness, long gas, out ApplicationEngine engine)
        {
            int offset;
            engine = null;
            ContractMethodDescriptor init = null;
            byte[] verification = witness.VerificationScript;
            if (verification.Length == 0)
            {
                ContractState cs = snapshot.Contracts.TryGet(hash);
                if (cs is null) return false;
                ContractMethodDescriptor md = cs.Manifest.Abi.GetMethod("verify");
                if (md is null) return false;
                verification = cs.Script;
                offset = md.Offset;
                init = cs.Manifest.Abi.GetMethod("_initialize");
            }
            else
            {
                if (NativeContract.IsNative(witness.ScriptHash)) return false;
                if (hash != witness.ScriptHash) return false;
                offset = 0;
            }
            engine = ApplicationEngine.Create(TriggerType.Verification, verifiable, snapshot?.Clone(), gas);
            CallFlags callFlags = witness.StateDependent ? CallFlags.AllowStates : CallFlags.None;
            ExecutionContext context = engine.LoadScript(verification, callFlags, offset);
            if (NativeContract.IsNative(witness.ScriptHash))
            {
                using ScriptBuilder sb = new ScriptBuilder();
                sb.Emit(OpCode.DEPTH, OpCode.PACK);
                sb.EmitPush("verify");
                engine.LoadScript(sb.ToArray(), CallFlags.None);
            }
            else if (init != null)
            {
                engine.LoadContext(context.Clone(init.Offset), false);
            }
            engine.LoadScript(witness.InvocationScript, CallFlags.None);
            return true;
        }

        internal static bool VerifyWitnesses(this IVerifiable verifiable, StoreView snapshot, long gas, WitnessFlag filter = WitnessFlag.All)
        {
            if (gas < 0) return false;
            if (gas > MaxVerificationGas) gas = MaxVerificationGas;

            UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying(snapshot);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != verifiable.Witnesses.Length) return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                WitnessFlag flag = verifiable.Witnesses[i].StateDependent ? WitnessFlag.StateDependent : WitnessFlag.StateIndependent;
                if (!filter.HasFlag(flag))
                {
                    gas -= verifiable.Witnesses[i].GasConsumed;
                    if (gas < 0) return false;
                    continue;
                }

                if (!TryCreateVerifyEngine(verifiable, snapshot?.Clone(), hashes[i], verifiable.Witnesses[i], gas, out var engine)) return false;

                using (engine)
                {
                    if (engine.Execute() == VMState.FAULT) return false;
                    if (engine.ResultStack.Count != 1 || !engine.ResultStack.Pop().GetBoolean()) return false;
                    gas -= engine.GasConsumed;
                    verifiable.Witnesses[i].GasConsumed = engine.GasConsumed;
                }
            }
            return true;
        }
    }
}
