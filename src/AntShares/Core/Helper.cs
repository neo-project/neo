using AntShares.Cryptography;
using AntShares.VM;
using AntShares.Wallets;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 包含一系列签名与验证的扩展方法
    /// </summary>
    public static class Helper
    {
        public static byte[] GetHashData(this ISignable signable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                signable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 根据传入的账户信息，对可签名的对象进行签名
        /// </summary>
        /// <param name="signable">要签名的数据</param>
        /// <param name="account">用于签名的账户</param>
        /// <returns>返回签名后的结果</returns>
        public static byte[] Sign(this ISignable signable, Account account)
        {
            using (account.Decrypt())
            {
                return Crypto.Default.Sign(signable.GetHashData(), account.PrivateKey, account.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        internal static bool VerifyScripts(this ISignable signable)
        {
            const int max_steps = 1200;
            UInt160[] hashes;
            try
            {
                hashes = signable.GetScriptHashesForVerifying();
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != signable.Scripts.Length) return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                byte[] redeem_script = signable.Scripts[i].RedeemScript;
                if (redeem_script.Length == 0)
                {
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        sb.EmitAppCall(hashes[i].ToArray());
                        redeem_script = sb.ToArray();
                    }
                }
                else
                {
                    if (hashes[i] != redeem_script.ToScriptHash()) return false;
                }
                int nOpCount = 0;
                ExecutionEngine engine = new ExecutionEngine(signable, Crypto.Default, Blockchain.Default, InterfaceEngine.Default);
                engine.LoadScript(redeem_script, false);
                engine.LoadScript(signable.Scripts[i].StackScript, true);
                while (!engine.State.HasFlag(VMState.HALT) && !engine.State.HasFlag(VMState.FAULT))
                {
                    if (engine.CurrentContext.InstructionPointer < engine.CurrentContext.Script.Length)
                    {
                        if (++nOpCount > max_steps) return false;
                        if (engine.CurrentContext.NextInstruction == OpCode.CHECKMULTISIG)
                        {
                            if (engine.EvaluationStack.Count == 0) return false;
                            int n = (int)engine.EvaluationStack.Peek().GetBigInteger();
                            if (n < 1) return false;
                            nOpCount += n;
                            if (nOpCount > max_steps) return false;
                        }
                    }
                    engine.StepInto();
                }
                if (engine.State.HasFlag(VMState.FAULT)) return false;
                if (engine.EvaluationStack.Count != 1 || !engine.EvaluationStack.Pop().GetBoolean()) return false;
            }
            return true;
        }
    }
}
