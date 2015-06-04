using System;
using System.IO;

namespace AntShares.Core.Scripts
{
    internal class ScriptBuilder : IDisposable
    {
        private MemoryStream ms = new MemoryStream();

        public ScriptBuilder Add(byte[] script)
        {
            ms.Write(script, 0, script.Length);
            return this;
        }

        public void Dispose()
        {
            ms.Dispose();
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
                Push(ScriptOp.OP_PUSHDATA1);
                ms.WriteByte((byte)data.Length);
                ms.Write(data, 0, data.Length);
            }
            else if (data.Length < 0x10000)
            {
                Push(ScriptOp.OP_PUSHDATA2);
                ms.Write(BitConverter.GetBytes((UInt16)data.Length), 0, 2);
                ms.Write(data, 0, data.Length);
            }
            else if (data.LongLength < 0x100000000L)
            {
                Push(ScriptOp.OP_PUSHDATA4);
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

        public ScriptBuilder Push(ScriptOp op)
        {
            ms.WriteByte((byte)op);
            return this;
        }

        public byte[] ToArray()
        {
            return ms.ToArray();
        }
    }
}
