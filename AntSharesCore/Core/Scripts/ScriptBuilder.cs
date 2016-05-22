using System;
using System.IO;
using System.Numerics;

namespace AntShares.Core.Scripts
{
    /// <summary>
    /// 脚本生成器
    /// </summary>
    public class ScriptBuilder : IDisposable
    {
        private MemoryStream ms = new MemoryStream();

        /// <summary>
        /// 添加操作符
        /// </summary>
        /// <param name="op">操作符</param>
        /// <returns>返回添加操作符之后的脚本生成器</returns>
        public ScriptBuilder Add(ScriptOp op)
        {
            ms.WriteByte((byte)op);
            return this;
        }

        /// <summary>
        /// 添加一段脚本
        /// </summary>
        /// <param name="script">脚本</param>
        /// <returns>返回添加脚本之后的脚本生成器</returns>
        public ScriptBuilder Add(byte[] script)
        {
            ms.Write(script, 0, script.Length);
            return this;
        }

        public void Dispose()
        {
            ms.Dispose();
        }

        /// <summary>
        /// 添加一段脚本，该脚本的作用是将一个整数压入栈中
        /// </summary>
        /// <param name="number">要压入栈中的整数</param>
        /// <returns>返回添加脚本之后的脚本生成器</returns>
        public ScriptBuilder Push(BigInteger number)
        {
            if (number == -1) return Add(ScriptOp.OP_1NEGATE);
            if (number == 0) return Add(ScriptOp.OP_0);
            if (number > 0 && number <= 16) return Add(ScriptOp.OP_1 - 1 + (byte)number);
            return Push(number.ToByteArray());
        }

        /// <summary>
        /// 添加一段脚本，该脚本的作用是将一个字节数组压入栈中
        /// </summary>
        /// <param name="data">要压入栈中的字节数组</param>
        /// <returns>返回添加脚本之后的脚本生成器</returns>
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
                ms.Write(BitConverter.GetBytes((ushort)data.Length), 0, 2);
                ms.Write(data, 0, data.Length);
            }
            else// if (data.Length < 0x100000000L)
            {
                Add(ScriptOp.OP_PUSHDATA4);
                ms.Write(BitConverter.GetBytes((uint)data.Length), 0, 4);
                ms.Write(data, 0, data.Length);
            }
            return this;
        }

        /// <summary>
        /// 添加一段脚本，该脚本的作用是将一个散列值压入栈中
        /// </summary>
        /// <param name="hash">要压入栈中的散列值</param>
        /// <returns>返回添加脚本之后的脚本生成器</returns>
        public ScriptBuilder Push(UIntBase hash)
        {
            return Push(hash.ToArray());
        }

        /// <summary>
        /// 获取脚本生成器中包含的脚本代码
        /// </summary>
        public byte[] ToArray()
        {
            return ms.ToArray();
        }
    }
}
