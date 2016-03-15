using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using BooleanStack = System.Collections.Generic.Stack<bool>;

namespace AntShares.Core.Scripts
{
    internal class ScriptEngine
    {
        private const int MAXSTEPS = 1200;

        private readonly Script script;
        private readonly ISignable signable;
        private readonly byte[] hash;

        private InterfaceEngine iEngine = null;
        private readonly Stack<StackItem> stack = new Stack<StackItem>();
        private readonly Stack<StackItem> altStack = new Stack<StackItem>();
        private readonly BooleanStack vfExec = new BooleanStack();
        private int nOpCount = 0;

        public ScriptEngine(Script script, ISignable signable)
        {
            this.script = script;
            this.signable = signable;
            this.hash = signable.GetHashForSigning();
        }

        public bool Execute()
        {
            try
            {
                if (!ExecuteScript(script.StackScript, true)) return false;
                if (!ExecuteScript(script.RedeemScript, false)) return false;
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException)
            {
                return false;
            }
            return stack.Count == 0 || (stack.Count == 1 && stack.Peek());
        }

        private bool ExecuteOp(ScriptOp opcode, BinaryReader opReader)
        {
            bool fExec = vfExec.All(p => p);
            if (!fExec && (opcode < ScriptOp.OP_IF || opcode > ScriptOp.OP_ENDIF))
                return true;
            if (opcode > ScriptOp.OP_16 && ++nOpCount > MAXSTEPS) return false;
            int remain = (int)(opReader.BaseStream.Length - opReader.BaseStream.Position);
            if (opcode >= ScriptOp.OP_PUSHBYTES1 && opcode <= ScriptOp.OP_PUSHBYTES75)
            {
                if (remain < (byte)opcode) return false;
                stack.Push(opReader.ReadBytes((byte)opcode));
                return true;
            }
            switch (opcode)
            {
                // Push value
                case ScriptOp.OP_0:
                    stack.Push(new byte[0]);
                    break;
                case ScriptOp.OP_PUSHDATA1:
                    {
                        if (remain < 1) return false;
                        byte length = opReader.ReadByte();
                        if (remain - 1 < length) return false;
                        stack.Push(opReader.ReadBytes(length));
                    }
                    break;
                case ScriptOp.OP_PUSHDATA2:
                    {
                        if (remain < 2) return false;
                        ushort length = opReader.ReadUInt16();
                        if (remain - 2 < length) return false;
                        stack.Push(opReader.ReadBytes(length));
                    }
                    break;
                case ScriptOp.OP_PUSHDATA4:
                    {
                        if (remain < 4) return false;
                        int length = opReader.ReadInt32();
                        if (remain - 4 < length) return false;
                        stack.Push(opReader.ReadBytes(length));
                    }
                    break;
                case ScriptOp.OP_1NEGATE:
                case ScriptOp.OP_1:
                case ScriptOp.OP_2:
                case ScriptOp.OP_3:
                case ScriptOp.OP_4:
                case ScriptOp.OP_5:
                case ScriptOp.OP_6:
                case ScriptOp.OP_7:
                case ScriptOp.OP_8:
                case ScriptOp.OP_9:
                case ScriptOp.OP_10:
                case ScriptOp.OP_11:
                case ScriptOp.OP_12:
                case ScriptOp.OP_13:
                case ScriptOp.OP_14:
                case ScriptOp.OP_15:
                case ScriptOp.OP_16:
                    stack.Push(opcode - ScriptOp.OP_1 + 1);
                    break;

                // Control
                case ScriptOp.OP_NOP:
                    break;
                case ScriptOp.OP_CALL:
                    if (remain < 1) return false;
                    if (iEngine == null)
                        iEngine = new InterfaceEngine(stack, altStack, signable);
                    return iEngine.ExecuteOp((InterfaceOp)opReader.ReadByte());
                case ScriptOp.OP_IF:
                case ScriptOp.OP_NOTIF:
                    {
                        bool fValue = false;
                        if (fExec)
                        {
                            if (stack.Count < 1) return false;
                            fValue = stack.Pop();
                            if (opcode == ScriptOp.OP_NOTIF)
                                fValue = !fValue;
                        }
                        vfExec.Push(fValue);
                    }
                    break;
                case ScriptOp.OP_ELSE:
                    if (vfExec.Count == 0) return false;
                    vfExec.Push(!vfExec.Pop());
                    break;
                case ScriptOp.OP_ENDIF:
                    if (vfExec.Count == 0) return false;
                    vfExec.Pop();
                    break;
                case ScriptOp.OP_VERIFY:
                    if (stack.Count < 1) return false;
                    if (stack.Peek().GetBooleanArray().All(p => p))
                        stack.Pop();
                    else
                        return false;
                    break;
                case ScriptOp.OP_RETURN:
                    return false;

                // Stack ops
                case ScriptOp.OP_TOALTSTACK:
                    if (stack.Count < 1) return false;
                    altStack.Push(stack.Pop());
                    break;
                case ScriptOp.OP_FROMALTSTACK:
                    if (altStack.Count < 1) return false;
                    stack.Push(altStack.Pop());
                    break;
                case ScriptOp.OP_2DROP:
                    if (stack.Count < 2) return false;
                    stack.Pop();
                    stack.Pop();
                    break;
                case ScriptOp.OP_2DUP:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Peek();
                        stack.Push(x2);
                        stack.Push(x1);
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_3DUP:
                    {
                        if (stack.Count < 3) return false;
                        StackItem x3 = stack.Pop();
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Peek();
                        stack.Push(x2);
                        stack.Push(x3);
                        stack.Push(x1);
                        stack.Push(x2);
                        stack.Push(x3);
                    }
                    break;
                case ScriptOp.OP_2OVER:
                    {
                        if (stack.Count < 4) return false;
                        StackItem x4 = stack.Pop();
                        StackItem x3 = stack.Pop();
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Peek();
                        stack.Push(x2);
                        stack.Push(x3);
                        stack.Push(x4);
                        stack.Push(x1);
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_2ROT:
                    {
                        if (stack.Count < 6) return false;
                        StackItem x6 = stack.Pop();
                        StackItem x5 = stack.Pop();
                        StackItem x4 = stack.Pop();
                        StackItem x3 = stack.Pop();
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        stack.Push(x3);
                        stack.Push(x4);
                        stack.Push(x5);
                        stack.Push(x6);
                        stack.Push(x1);
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_2SWAP:
                    {
                        if (stack.Count < 4) return false;
                        StackItem x4 = stack.Pop();
                        StackItem x3 = stack.Pop();
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        stack.Push(x3);
                        stack.Push(x4);
                        stack.Push(x1);
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_IFDUP:
                    if (stack.Count < 1) return false;
                    if (stack.Peek())
                        stack.Push(stack.Peek());
                    break;
                case ScriptOp.OP_DEPTH:
                    stack.Push(stack.Count);
                    break;
                case ScriptOp.OP_DROP:
                    if (stack.Count < 1) return false;
                    stack.Pop();
                    break;
                case ScriptOp.OP_DUP:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Peek());
                    break;
                case ScriptOp.OP_NIP:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        stack.Pop();
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_OVER:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Peek();
                        stack.Push(x2);
                        stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_PICK:
                    {
                        if (stack.Count < 2) return false;
                        int n = (int)(BigInteger)stack.Pop();
                        if (n < 0) return false;
                        if (stack.Count < n + 1) return false;
                        StackItem[] buffer = new StackItem[n];
                        for (int i = 0; i < n; i++)
                            buffer[i] = stack.Pop();
                        StackItem xn = stack.Peek();
                        for (int i = n - 1; i >= 0; i--)
                            stack.Push(buffer[i]);
                        stack.Push(xn);
                    }
                    break;
                case ScriptOp.OP_ROLL:
                    {
                        if (stack.Count < 2) return false;
                        int n = (int)(BigInteger)stack.Pop();
                        if (n < 0) return false;
                        if (n == 0) return true;
                        if (stack.Count < n + 1) return false;
                        StackItem[] buffer = new StackItem[n];
                        for (int i = 0; i < n; i++)
                            buffer[i] = stack.Pop();
                        StackItem xn = stack.Pop();
                        for (int i = n - 1; i >= 0; i--)
                            stack.Push(buffer[i]);
                        stack.Push(xn);
                    }
                    break;
                case ScriptOp.OP_ROT:
                    {
                        if (stack.Count < 3) return false;
                        StackItem x3 = stack.Pop();
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        stack.Push(x2);
                        stack.Push(x3);
                        stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_SWAP:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        stack.Push(x2);
                        stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_TUCK:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        stack.Push(x2);
                        stack.Push(x1);
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_CAT:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        byte[][] b1 = x1.GetBytesArray();
                        byte[][] b2 = x2.GetBytesArray();
                        if (b1.Length != b2.Length) return false;
                        byte[][] r = b1.Zip(b2, (p1, p2) => p1.Concat(p2).ToArray()).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SUBSTR:
                    {
                        if (stack.Count < 3) return false;
                        int count = (int)(BigInteger)stack.Pop();
                        if (count < 0) return false;
                        int index = (int)(BigInteger)stack.Pop();
                        if (index < 0) return false;
                        StackItem x = stack.Pop();
                        byte[][] s = x.GetBytesArray();
                        s = s.Select(p => p.Skip(index).Take(count).ToArray()).ToArray();
                        if (x.IsArray)
                            stack.Push(s);
                        else
                            stack.Push(s[0]);
                    }
                    break;
                case ScriptOp.OP_LEFT:
                    {
                        if (stack.Count < 2) return false;
                        int count = (int)(BigInteger)stack.Pop();
                        if (count < 0) return false;
                        StackItem x = stack.Pop();
                        byte[][] s = x.GetBytesArray();
                        s = s.Select(p => p.Take(count).ToArray()).ToArray();
                        if (x.IsArray)
                            stack.Push(s);
                        else
                            stack.Push(s[0]);
                    }
                    break;
                case ScriptOp.OP_RIGHT:
                    {
                        if (stack.Count < 2) return false;
                        int count = (int)(BigInteger)stack.Pop();
                        if (count < 0) return false;
                        StackItem x = stack.Pop();
                        byte[][] s = x.GetBytesArray();
                        if (s.Any(p => p.Length < count)) return false;
                        s = s.Select(p => p.Skip(p.Length - count).ToArray()).ToArray();
                        if (x.IsArray)
                            stack.Push(s);
                        else
                            stack.Push(s[0]);
                    }
                    break;
                case ScriptOp.OP_SIZE:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Peek();
                        int[] r = x.GetBytesArray().Select(p => p.Length).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;

                // Bitwise logic
                case ScriptOp.OP_INVERT:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => ~p).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_AND:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 & p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_OR:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 | p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_XOR:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 ^ p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_EQUAL:
                case ScriptOp.OP_EQUALVERIFY:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        byte[][] b1 = x1.GetBytesArray();
                        byte[][] b2 = x2.GetBytesArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1.SequenceEqual(p2)).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                        if (opcode == ScriptOp.OP_EQUALVERIFY)
                            return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    }
                    break;

                // Numeric
                case ScriptOp.OP_1ADD:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p + BigInteger.One).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_1SUB:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p - BigInteger.One).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_2MUL:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p << 1).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_2DIV:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p >> 1).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NEGATE:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => -p).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_ABS:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => BigInteger.Abs(p)).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NOT:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        bool[] r = x.GetBooleanArray().Select(p => !p).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_0NOTEQUAL:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        bool[] r = x.GetIntArray().Select(p => p != BigInteger.Zero).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_ADD:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 + p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SUB:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 - p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MUL:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 * p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_DIV:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 / p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MOD:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 % p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_LSHIFT:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 << (int)p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_RSHIFT:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 >> (int)p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_BOOLAND:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        bool[] b1 = x1.GetBooleanArray();
                        bool[] b2 = x2.GetBooleanArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 && p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_BOOLOR:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        bool[] b1 = x1.GetBooleanArray();
                        bool[] b2 = x2.GetBooleanArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 || p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NUMEQUAL:
                case ScriptOp.OP_NUMEQUALVERIFY:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 == p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                        if (opcode == ScriptOp.OP_NUMEQUALVERIFY)
                            return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    }
                    break;
                case ScriptOp.OP_NUMNOTEQUAL:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 != p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_LESSTHAN:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 < p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_GREATERTHAN:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 > p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_LESSTHANOREQUAL:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 <= p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_GREATERTHANOREQUAL:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 >= p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MIN:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Min(p1, p2)).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MAX:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return false;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Max(p1, p2)).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_WITHIN:
                    {
                        if (stack.Count < 3) return false;
                        BigInteger b = (BigInteger)stack.Pop();
                        BigInteger a = (BigInteger)stack.Pop();
                        BigInteger x = (BigInteger)stack.Pop();
                        stack.Push(a <= x && x < b);
                    }
                    break;

                // Crypto
                case ScriptOp.OP_RIPEMD160:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.RIPEMD160()).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SHA1:
                    using (SHA1Managed sha = new SHA1Managed())
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => sha.ComputeHash(p)).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SHA256:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.Sha256()).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_HASH160:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.Sha256().RIPEMD160()).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_HASH256:
                    {
                        if (stack.Count < 1) return false;
                        StackItem x = stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.Sha256().Sha256()).ToArray();
                        if (x.IsArray)
                            stack.Push(r);
                        else
                            stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_CHECKSIG:
                case ScriptOp.OP_CHECKSIGVERIFY:
                    {
                        if (stack.Count < 2) return false;
                        byte[] pubkey = (byte[])stack.Pop();
                        byte[] signature = (byte[])stack.Pop();
                        stack.Push(VerifySignature(hash, signature, pubkey));
                        if (opcode == ScriptOp.OP_CHECKSIGVERIFY)
                            return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    }
                    break;
                case ScriptOp.OP_CHECKMULTISIG:
                case ScriptOp.OP_CHECKMULTISIGVERIFY:
                    {
                        if (stack.Count < 4) return false;
                        int n = (int)(BigInteger)stack.Pop();
                        if (n < 1) return false;
                        if (stack.Count < n + 2) return false;
                        nOpCount += n;
                        if (nOpCount > MAXSTEPS) return false;
                        byte[][] pubkeys = new byte[n][];
                        for (int i = 0; i < n; i++)
                        {
                            pubkeys[i] = (byte[])stack.Pop();
                        }
                        int m = (int)(BigInteger)stack.Pop();
                        if (m < 1 || m > n) return false;
                        if (stack.Count < m) return false;
                        List<byte[]> signatures = new List<byte[]>();
                        while (stack.Count > 0)
                        {
                            byte[] signature = (byte[])stack.Pop();
                            if (signature.Length == 0) break;
                            signatures.Add(signature);
                        }
                        if (signatures.Count < m || signatures.Count > n) return false;
                        bool fSuccess = true;
                        for (int i = 0, j = 0; fSuccess && i < signatures.Count && j < n;)
                        {
                            if (VerifySignature(hash, signatures[i], pubkeys[j]))
                                i++;
                            j++;
                            if (i >= m) break;
                            if (signatures.Count - i > n - j)
                                fSuccess = false;
                        }
                        stack.Push(fSuccess);
                        if (opcode == ScriptOp.OP_CHECKMULTISIGVERIFY)
                            return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    }
                    break;

                //case ScriptOp.OP_EVAL:
                //    if (stack.Count < 1) return false;
                //    if (!ExecuteScript((byte[])stack.Pop(), false))
                //        return false;
                //    break;

                // Array
                case ScriptOp.OP_ARRAYSIZE:
                    {
                        if (stack.Count < 1) return false;
                        StackItem arr = stack.Pop();
                        if (arr.IsArray)
                            stack.Push(arr.Count);
                        else
                            stack.Push(1);
                    }
                    break;
                case ScriptOp.OP_PACK:
                    {
                        if (stack.Count < 1) return false;
                        int c = (int)(BigInteger)stack.Pop();
                        if (stack.Count < c) return false;
                        StackItem[] arr = new StackItem[c];
                        while (c-- > 0)
                        {
                            arr[c] = stack.Pop();
                            if (arr[c].IsArray) return false;
                        }
                        stack.Push(new StackItem(arr));
                    }
                    break;
                case ScriptOp.OP_UNPACK:
                    {
                        if (stack.Count < 1) return false;
                        StackItem arr = stack.Pop();
                        if (!arr.IsArray) return false;
                        foreach (StackItem item in arr)
                            stack.Push(item);
                        stack.Push(arr.Count);
                    }
                    break;
                case ScriptOp.OP_DISTINCT:
                    if (stack.Count < 1) return false;
                    stack.Push(new StackItem(stack.Pop().Distinct()));
                    break;
                case ScriptOp.OP_SORT:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Pop().GetIntArray().OrderBy(p => p).ToArray());
                    break;
                case ScriptOp.OP_REVERSE:
                    if (stack.Count < 1) return false;
                    stack.Push(new StackItem(stack.Pop().Reverse()));
                    break;
                case ScriptOp.OP_CONCAT:
                    {
                        if (stack.Count < 1) return false;
                        int c = (int)(BigInteger)stack.Pop();
                        if (stack.Count < c) return false;
                        IEnumerable<StackItem> items = Enumerable.Empty<StackItem>();
                        while (c-- > 0)
                            items = stack.Pop().Concat(items);
                        stack.Push(new StackItem(items));
                    }
                    break;
                case ScriptOp.OP_UNION:
                    {
                        if (stack.Count < 1) return false;
                        int c = (int)(BigInteger)stack.Pop();
                        if (stack.Count < c) return false;
                        IEnumerable<StackItem> items = Enumerable.Empty<StackItem>();
                        while (c-- > 0)
                            items = stack.Pop().Union(items);
                        stack.Push(new StackItem(items));
                    }
                    break;
                case ScriptOp.OP_INTERSECT:
                    {
                        if (stack.Count < 1) return false;
                        int c = (int)(BigInteger)stack.Pop();
                        if (stack.Count < c) return false;
                        IEnumerable<StackItem> items = Enumerable.Empty<StackItem>();
                        while (c-- > 0)
                            items = stack.Pop().Intersect(items);
                        stack.Push(new StackItem(items));
                    }
                    break;
                case ScriptOp.OP_EXCEPT:
                    {
                        if (stack.Count < 2) return false;
                        StackItem x2 = stack.Pop();
                        StackItem x1 = stack.Pop();
                        stack.Push(new StackItem(x1.Except(x2)));
                    }
                    break;
                case ScriptOp.OP_TAKE:
                    {
                        if (stack.Count < 2) return false;
                        int count = (int)(BigInteger)stack.Pop();
                        stack.Push(new StackItem(stack.Pop().Take(count)));
                    }
                    break;
                case ScriptOp.OP_SKIP:
                    {
                        if (stack.Count < 2) return false;
                        int count = (int)(BigInteger)stack.Pop();
                        stack.Push(new StackItem(stack.Pop().Skip(count)));
                    }
                    break;
                case ScriptOp.OP_PICKITEM:
                    {
                        if (stack.Count < 2) return false;
                        int index = (int)(BigInteger)stack.Pop();
                        StackItem arr = stack.Pop();
                        if (arr.Count <= index)
                            stack.Push(new StackItem((byte[])null));
                        else
                            stack.Push(arr[index]);
                    }
                    break;
                case ScriptOp.OP_ALL:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Pop().All(p => p));
                    break;
                case ScriptOp.OP_ANY:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Pop().Any(p => p));
                    break;
                case ScriptOp.OP_SUM:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Pop().Aggregate(BigInteger.Zero, (s, p) => s + (BigInteger)p));
                    break;
                case ScriptOp.OP_AVERAGE:
                    {
                        if (stack.Count < 1) return false;
                        StackItem arr = stack.Pop();
                        if (arr.Count == 0) return false;
                        stack.Push(arr.Aggregate(BigInteger.Zero, (s, p) => s + (BigInteger)p, p => p / arr.Count));
                    }
                    break;
                case ScriptOp.OP_MAXITEM:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Pop().GetIntArray().Max());
                    break;
                case ScriptOp.OP_MINITEM:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.Pop().GetIntArray().Min());
                    break;

                default:
                    return false;
            }
            return true;
        }

        private bool ExecuteScript(byte[] script, bool push_only)
        {
            using (MemoryStream ms = new MemoryStream(script, false))
            using (BinaryReader opReader = new BinaryReader(ms))
            {
                while (opReader.BaseStream.Position < script.Length)
                {
                    ScriptOp opcode = (ScriptOp)opReader.ReadByte();
                    if (push_only && opcode > ScriptOp.OP_16) return false;
                    if (!ExecuteOp(opcode, opReader)) return false;
                }
            }
            return true;
        }

        private static bool VerifySignature(byte[] hash, byte[] signature, byte[] pubkey)
        {
            const int ECDSA_PUBLIC_P256_MAGIC = 0x31534345;
            try
            {
                pubkey = ECPoint.DecodePoint(pubkey, ECCurve.Secp256r1).EncodePoint(false).Skip(1).ToArray();
            }
            catch
            {
                return false;
            }
            pubkey = BitConverter.GetBytes(ECDSA_PUBLIC_P256_MAGIC).Concat(BitConverter.GetBytes(32)).Concat(pubkey).ToArray();
            using (CngKey key = CngKey.Import(pubkey, CngKeyBlobFormat.EccPublicBlob))
            using (ECDsaCng ecdsa = new ECDsaCng(key))
            {
                return ecdsa.VerifyHash(hash, signature);
            }
        }
    }
}
