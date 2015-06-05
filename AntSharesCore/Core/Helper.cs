using AntShares.Core.Scripts;
using AntShares.Cryptography;
using System;
using System.Linq;

namespace AntShares.Core
{
    public static class Helper
    {
        private const byte CoinVersion = 0x17;

        public static string ToAddress(this UInt160 hash)
        {
            byte[] data = new byte[] { CoinVersion }.Concat(hash.ToArray()).ToArray();
            return Base58.Encode(data.Concat(data.Sha256().Sha256().Take(4)).ToArray());
        }

        public static UInt160 ToScriptHash(this string address)
        {
            byte[] data = Base58.Decode(address);
            if (data.Length != 25)
                throw new FormatException();
            if (data[0] != CoinVersion)
                throw new FormatException();
            if (!data.Take(21).Sha256().Sha256().Take(4).SequenceEqual(data.Skip(21)))
                throw new FormatException();
            return new UInt160(data.Skip(1).Take(20).ToArray());
        }

        internal static bool Verify(this ISignable signable)
        {
            UInt160[] hashes = signable.GetScriptHashesForVerifying();
            byte[][] scripts = signable.GetScriptsForVerifying();
            if (hashes.Length != scripts.Length)
                return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    byte[] script = sb.Add(scripts[i]).Push(ScriptOp.OP_DUP).Push(ScriptOp.OP_HASH160).Push(hashes[i]).Push(ScriptOp.OP_EQUALVERIFY).Push(ScriptOp.OP_EVAL).ToArray();
                    if (!ScriptEngine.Execute(script, signable.GetHashForSigning()))
                        return false;
                }
            }
            return true;
        }
    }
}
