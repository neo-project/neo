using Neo.Cryptography;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    /// <summary>
    /// 包含一系列签名与验证的扩展方法
    /// </summary>
    public static class Helper
    {
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 根据传入的账户信息，对可签名的对象进行签名
        /// </summary>
        /// <param name="verifiable">要签名的数据</param>
        /// <param name="key">用于签名的账户</param>
        /// <returns>返回签名后的结果</returns>
        public static byte[] Sign(this IVerifiable verifiable, KeyPair key)
        {
            using (key.Decrypt())
            {
                return Crypto.Default.Sign(verifiable.GetHashData(), key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
            }
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        internal static bool VerifyScripts(this IVerifiable verifiable)
        {
			Console.WriteLine("VerifyScripts 0");
			UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying();
            }
            catch (InvalidOperationException)
            {
				Console.WriteLine("VerifyScripts 1");
				return false;
            }
			Console.WriteLine("VerifyScripts 2");
			if (hashes.Length != verifiable.Scripts.Length) return false;
			Console.WriteLine("VerifyScripts 3");
			for (int i = 0; i < hashes.Length; i++)
            {
                byte[] verification = verifiable.Scripts[i].VerificationScript;
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
                    Console.WriteLine($"VerifyScripts 4 {i}");

					if (hashes[i] != verification.ToScriptHash()) return false;
                }
                ApplicationEngine engine = new ApplicationEngine(verifiable, Blockchain.Default, StateReader.Default, Fixed8.Zero);
                engine.LoadScript(verification, false);
                engine.LoadScript(verifiable.Scripts[i].InvocationScript, true);
				Console.WriteLine("VerifyScripts 5");
				if (!engine.Execute()) return false;
				Console.WriteLine("VerifyScripts 6");
				if (engine.EvaluationStack.Count != 1 || !engine.EvaluationStack.Pop().GetBoolean()) return false;
				Console.WriteLine("VerifyScripts 7");
			}
			Console.WriteLine("VerifyScripts 8");
			return true;
        }
    }
}
