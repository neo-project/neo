using AntShares.Cryptography;
using System;
using System.IO;
using System.Numerics;

namespace AntShares.Core.Scripts
{
    public class ScriptBuilder : IDisposable
    {
        private MemoryStream ms = new MemoryStream();

        public ScriptBuilder Add(ScriptOp op)
        {
            ms.WriteByte((byte)op);
            return this;
        }

        public ScriptBuilder Add(byte[] script)
        {
            ms.Write(script, 0, script.Length);
            return this;
        }

        public static byte[] CreateRedeemScript(int m, params Secp256r1Point[] publicKeys)
        {
            if (!(1 <= m && m <= publicKeys.Length && publicKeys.Length <= 1024))
                throw new ArgumentException();
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.Push(m);
                for (int i = 0; i < publicKeys.Length; i++)
                {
                    sb.Push(publicKeys[i].EncodePoint(true));
                }
                sb.Push(publicKeys.Length);
                sb.Add(ScriptOp.OP_CHECKMULTISIG);
                return sb.ToArray();
            }
        }

        public void Dispose()
        {
            ms.Dispose();
        }

        public ScriptBuilder Push(BigInteger number)
        {
            if (number == -1) return Add(ScriptOp.OP_1NEGATE);
            if (number == 0) return Add(ScriptOp.OP_0);
            if (number > 0 && number <= 16) return Add(ScriptOp.OP_1 - 1 + (byte)number);
            return Push(number.ToByteArray());
        }

        public ScriptBuilder Push(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException();
            if (data.Length <= (int)ScriptOp.OP_PUSHBYTES75)
            {
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x100)
            {
                Add(ScriptOp.OP_PUSHDATA1);
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x10000)
            {
                Add(ScriptOp.OP_PUSHDATA2);
                ms.Write(BitConverter.GetBytes((UInt16)data.Length), 0, 2);
                ms.Write(data, 0, data.Length);
            }
            else if (data.LongLength < 0x100000000L)
            {
                Add(ScriptOp.OP_PUSHDATA4);
                ms.Write(BitConverter.GetBytes((UInt32)data.Length), 0, 4);
                ms.Write(data, 0, data.Length);
            }
            else
            {
                throw new ArgumentException();
            }
            return this;
        }

        public ScriptBuilder Push(UIntBase hash)
        {
            return Push(hash.ToArray());
        }

        public byte[] ToArray()
        {
            return ms.ToArray();
        }
    }
}
