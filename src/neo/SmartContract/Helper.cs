using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    public static class Helper
    {
        public const long MaxVerificationGas = 0_50000000;

        public static long SignatureContractCost() =>
            ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * 2 +
            ApplicationEngine.OpCodePrices[OpCode.PUSHNULL] +
            ApplicationEngine.OpCodePrices[OpCode.SYSCALL] +
            ApplicationEngine.ECDsaVerifyPrice;

        public static long MultiSignatureContractCost(int m, int n) =>
            ApplicationEngine.OpCodePrices[OpCode.PUSHDATA1] * (m + n) +
            ApplicationEngine.OpCodePrices[OpCode.PUSHINT8] * 2 +
            ApplicationEngine.OpCodePrices[OpCode.PUSHNULL] +
            ApplicationEngine.OpCodePrices[OpCode.SYSCALL] +
            ApplicationEngine.ECDsaVerifyPrice * n;

        public static UInt160 GetContractHash(UInt160 sender, uint nefCheckSum, string name)
        {
            using var sb = new ScriptBuilder();
            sb.Emit(OpCode.ABORT);
            sb.EmitPush(sender);
            sb.EmitPush(nefCheckSum);
            sb.EmitPush(name);

            return sb.ToArray().ToScriptHash();
        }

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

        public static T ToInteroperable<T>(this StackItem item) where T : IInteroperable, new()
        {
            T t = new T();
            t.FromStackItem(item);
            return t;
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Hash160(script));
        }

        public static UInt160 ToScriptHash(this ReadOnlySpan<byte> script)
        {
            return new UInt160(Crypto.Hash160(script));
        }

        public static bool VerifyWitnesses(this IVerifiable verifiable, DataCache snapshot, long gas)
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
                if (!verifiable.VerifyWitness(snapshot, hashes[i], verifiable.Witnesses[i], gas, out long fee))
                    return false;
                gas -= fee;
            }
            return true;
        }

        internal static bool VerifyWitness(this IVerifiable verifiable, DataCache snapshot, UInt160 hash, Witness witness, long gas, out long fee)
        {
            fee = 0;
            Script invocationScript;
            try
            {
                invocationScript = new Script(witness.InvocationScript, true);
            }
            catch (BadScriptException)
            {
                return false;
            }
            using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, verifiable, snapshot?.CreateSnapshot(), null, gas))
            {
                if (witness.VerificationScript.Length == 0)
                {
                    ContractState cs = NativeContract.ContractManagement.GetContract(snapshot, hash);
                    if (cs is null) return false;
                    ContractMethodDescriptor md = cs.Manifest.Abi.GetMethod("verify", -1);
                    if (md?.ReturnType != ContractParameterType.Boolean) return false;
                    engine.LoadContract(cs, md, CallFlags.ReadOnly);
                }
                else
                {
                    if (NativeContract.IsNative(hash)) return false;
                    if (hash != witness.ScriptHash) return false;
                    Script verificationScript;
                    try
                    {
                        verificationScript = new Script(witness.VerificationScript, true);
                    }
                    catch (BadScriptException)
                    {
                        return false;
                    }
                    engine.LoadScript(verificationScript, initialPosition: 0, configureState: p =>
                    {
                        p.CallFlags = CallFlags.ReadOnly;
                        p.ScriptHash = hash;
                    });
                }

                engine.LoadScript(invocationScript, configureState: p => p.CallFlags = CallFlags.None);

                if (engine.Execute() == VMState.FAULT) return false;
                if (!engine.ResultStack.Peek().GetBoolean()) return false;
                fee = engine.GasConsumed;
            }
            return true;
        }
    }
}
