using Neo.Cryptography;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace Neo.SmartContract
{
    public static class Helper
    {
        private static readonly ConcurrentDictionary<string, uint> MethodHashes
            = new ConcurrentDictionary<string, uint>();

        public static bool IsMultiSigContract(this byte[] script)
        {
            int m, n = 0;
            int i = 0;
            if (script.Length < 37) return false;
            if (script[i] > (byte)OpCode.PUSH16) return false;
            if (script[i] < (byte)OpCode.PUSH1 && script[i] != 1 && script[i] != 2) return false;
            switch (script[i])
            {
                case 1:
                    m = script[++i];
                    ++i;
                    break;
                case 2:
                    m = script.ToUInt16(++i);
                    i += 2;
                    break;
                default:
                    m = script[i++] - 80;
                    break;
            }
            if (m < 1 || m > 1024) return false;
            while (script[i] == 33)
            {
                i += 34;
                if (script.Length <= i) return false;
                ++n;
            }
            if (n < m || n > 1024) return false;
            switch (script[i])
            {
                case 1:
                    if (n != script[++i]) return false;
                    ++i;
                    break;
                case 2:
                    if (script.Length < i + 3 || n != script.ToUInt16(++i)) return false;
                    i += 2;
                    break;
                default:
                    if (n != script[i++] - 80) return false;
                    break;
            }
            if (script[i++] != (byte)OpCode.CHECKMULTISIG) return false;
            if (script.Length != i) return false;
            return true;
        }

        public static bool IsSignatureContract(this byte[] script)
        {
            if (script.Length != 35) return false;
            if (script[0] != 33 || script[34] != (byte)OpCode.CHECKSIG)
                return false;
            return true;
        }

        public static bool IsStandardContract(this byte[] script)
        {
            return script.IsSignatureContract() || script.IsMultiSigContract();
        }

        public static uint ToInteropMethodHash(this string method)
        {
            return MethodHashes.GetOrAdd(method, p => BitConverter.ToUInt32(Encoding.ASCII.GetBytes(p).Sha256(), 0));
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        internal static bool VerifyWitnesses(this IVerifiable verifiable, Snapshot snapshot)
        {
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
                byte[] verification = verifiable.Witnesses[i].VerificationScript;
                if (verification.Length == 0)
                {
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        sb.EmitAppCall(hashes[i].ToArray());
                        verification = sb.ToArray();
                    }
                }
                else
                {
                    if (hashes[i] != verifiable.Witnesses[i].ScriptHash) return false;
                }
                using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, verifiable, snapshot, Fixed8.Zero))
                {
                    engine.LoadScript(verification);
                    engine.LoadScript(verifiable.Witnesses[i].InvocationScript);
                    engine.Execute();
                    if (engine.State.HasFlag(VMState.FAULT)) return false;
                    if (engine.ResultStack.Count != 1 || !engine.ResultStack.Pop().GetBoolean()) return false;
                }
            }
            return true;
        }
    }
}
