using System.Collections.Generic;
using System.Numerics;

namespace AntShares.Core.Scripts
{
    internal class Stack
    {
        private Stack<byte[]> stack = new Stack<byte[]>();

        public int Count
        {
            get
            {
                return stack.Count;
            }
        }

        private BigInteger CastToBigInteger(byte[] value)
        {
            return new BigInteger(value);
        }

        private bool CastToBool(byte[] value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0)
                {
                    if (i == value.Length - 1 && value[i] == 0x80)
                        return false;
                    return true;
                }
            }
            return false;
        }

        public BigInteger PeekBigInteger()
        {
            return CastToBigInteger(stack.Peek());
        }

        public bool PeekBool()
        {
            return CastToBool(stack.Peek());
        }

        public byte[] PeekBytes()
        {
            return stack.Peek();
        }

        public BigInteger PopBigInteger()
        {
            return CastToBigInteger(stack.Pop());
        }

        public bool PopBool()
        {
            return CastToBool(stack.Pop());
        }

        public byte[] PopBytes()
        {
            return stack.Pop();
        }

        public void Push(BigInteger value)
        {
            stack.Push(value.ToByteArray());
        }

        public void Push(bool value)
        {
            if (value)
                stack.Push(new byte[] { 1 });
            else
                stack.Push(new byte[0]);
        }

        public void Push(byte[] value)
        {
            stack.Push(value);
        }
    }
}
