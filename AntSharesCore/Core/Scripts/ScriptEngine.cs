using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace AntShares.Core.Scripts
{
    public class ScriptEngine
    {
        private const int MAXSTEPS = 1200;

        private readonly Script script;
        private readonly byte[] hash;
        private readonly IApiService service;
        private int nOpCount = 0;

        public Stack<StackItem> Stack { get; } = new Stack<StackItem>();
        public Stack<StackItem> AltStack { get; } = new Stack<StackItem>();
        public ISignable Signable { get; private set; }
        public byte[] ExecutingScript { get; private set; }

        public ScriptEngine(Script script, ISignable signable, IApiService service = null)
        {
            this.script = script;
            this.Signable = signable;
            this.hash = signable.GetHashForSigning();
            this.service = service;
        }

        public bool Execute()
        {
            if (!ExecuteScript(script.StackScript, true)) return false;
            if (!ExecuteScript(script.RedeemScript, false)) return false;
            return Stack.Count == 1 && Stack.Pop();
        }

        private VMState ExecuteOp(ScriptOp opcode, BinaryReader opReader)
        {
            if (opcode > ScriptOp.OP_16 && ++nOpCount > MAXSTEPS) return VMState.FAULT;
            if (opcode >= ScriptOp.OP_PUSHBYTES1 && opcode <= ScriptOp.OP_PUSHBYTES75)
            {
                Stack.Push(opReader.ReadBytes((byte)opcode));
                return VMState.NONE;
            }
            switch (opcode)
            {
                // Push value
                case ScriptOp.OP_0:
                    Stack.Push(new byte[0]);
                    break;
                case ScriptOp.OP_PUSHDATA1:
                    Stack.Push(opReader.ReadBytes(opReader.ReadByte()));
                    break;
                case ScriptOp.OP_PUSHDATA2:
                    Stack.Push(opReader.ReadBytes(opReader.ReadUInt16()));
                    break;
                case ScriptOp.OP_PUSHDATA4:
                    Stack.Push(opReader.ReadBytes(opReader.ReadInt32()));
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
                    Stack.Push(opcode - ScriptOp.OP_1 + 1);
                    break;

                // Control
                case ScriptOp.OP_NOP:
                    break;
                case ScriptOp.OP_JMP:
                case ScriptOp.OP_JMPIF:
                case ScriptOp.OP_JMPIFNOT:
                    {
                        int offset = opReader.ReadInt16() - 3;
                        int offset_new = (int)opReader.BaseStream.Position + offset;
                        if (offset_new < 0 || offset_new > opReader.BaseStream.Length)
                            return VMState.FAULT;
                        bool fValue = true;
                        if (opcode > ScriptOp.OP_JMP)
                        {
                            if (Stack.Count < 1) return VMState.FAULT;
                            fValue = Stack.Pop();
                            if (opcode == ScriptOp.OP_JMPIFNOT)
                                fValue = !fValue;
                        }
                        if (fValue)
                            opReader.BaseStream.Seek(offset_new, SeekOrigin.Begin);
                    }
                    break;
                case ScriptOp.OP_CALL:
                    Stack.Push(opReader.BaseStream.Position + 2);
                    return ExecuteOp(ScriptOp.OP_JMP, opReader);
                case ScriptOp.OP_RET:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem result = Stack.Pop();
                        int position = (int)(BigInteger)Stack.Pop();
                        if (position < 0 || position > opReader.BaseStream.Length)
                            return VMState.FAULT;
                        Stack.Push(result);
                        opReader.BaseStream.Seek(position, SeekOrigin.Begin);
                    }
                    break;
                case ScriptOp.OP_APPCALL:
                    {
                        UInt160 hash = opReader.ReadSerializable<UInt160>();
                        byte[] script = Blockchain.Default?.GetContract(hash);
                        if (script == null) return VMState.FAULT;
                        return ExecuteScript(script, false) ? VMState.NONE : VMState.FAULT;
                    }
                case ScriptOp.OP_SYSCALL:
                    if (service == null) return VMState.FAULT;
                    return service.Invoke(opReader.ReadVarString(), this) ? VMState.NONE : VMState.FAULT;
                case ScriptOp.OP_HALTIFNOT:
                    if (Stack.Count < 1) return VMState.FAULT;
                    if (Stack.Peek().GetBooleanArray().All(p => p))
                        Stack.Pop();
                    else
                        return VMState.HALT;
                    break;
                case ScriptOp.OP_HALT:
                    return VMState.HALT;

                // Stack ops
                case ScriptOp.OP_TOALTSTACK:
                    if (Stack.Count < 1) return VMState.FAULT;
                    AltStack.Push(Stack.Pop());
                    break;
                case ScriptOp.OP_FROMALTSTACK:
                    if (AltStack.Count < 1) return VMState.FAULT;
                    Stack.Push(AltStack.Pop());
                    break;
                case ScriptOp.OP_2DROP:
                    if (Stack.Count < 2) return VMState.FAULT;
                    Stack.Pop();
                    Stack.Pop();
                    break;
                case ScriptOp.OP_2DUP:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Peek();
                        Stack.Push(x2);
                        Stack.Push(x1);
                        Stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_3DUP:
                    {
                        if (Stack.Count < 3) return VMState.FAULT;
                        StackItem x3 = Stack.Pop();
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Peek();
                        Stack.Push(x2);
                        Stack.Push(x3);
                        Stack.Push(x1);
                        Stack.Push(x2);
                        Stack.Push(x3);
                    }
                    break;
                case ScriptOp.OP_2OVER:
                    {
                        if (Stack.Count < 4) return VMState.FAULT;
                        StackItem x4 = Stack.Pop();
                        StackItem x3 = Stack.Pop();
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Peek();
                        Stack.Push(x2);
                        Stack.Push(x3);
                        Stack.Push(x4);
                        Stack.Push(x1);
                        Stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_2ROT:
                    {
                        if (Stack.Count < 6) return VMState.FAULT;
                        StackItem x6 = Stack.Pop();
                        StackItem x5 = Stack.Pop();
                        StackItem x4 = Stack.Pop();
                        StackItem x3 = Stack.Pop();
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        Stack.Push(x3);
                        Stack.Push(x4);
                        Stack.Push(x5);
                        Stack.Push(x6);
                        Stack.Push(x1);
                        Stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_2SWAP:
                    {
                        if (Stack.Count < 4) return VMState.FAULT;
                        StackItem x4 = Stack.Pop();
                        StackItem x3 = Stack.Pop();
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        Stack.Push(x3);
                        Stack.Push(x4);
                        Stack.Push(x1);
                        Stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_IFDUP:
                    if (Stack.Count < 1) return VMState.FAULT;
                    if (Stack.Peek())
                        Stack.Push(Stack.Peek());
                    break;
                case ScriptOp.OP_DEPTH:
                    Stack.Push(Stack.Count);
                    break;
                case ScriptOp.OP_DROP:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Pop();
                    break;
                case ScriptOp.OP_DUP:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Peek());
                    break;
                case ScriptOp.OP_NIP:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        Stack.Pop();
                        Stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_OVER:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Peek();
                        Stack.Push(x2);
                        Stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_PICK:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int n = (int)(BigInteger)Stack.Pop();
                        if (n < 0) return VMState.FAULT;
                        if (Stack.Count < n + 1) return VMState.FAULT;
                        StackItem[] buffer = new StackItem[n];
                        for (int i = 0; i < n; i++)
                            buffer[i] = Stack.Pop();
                        StackItem xn = Stack.Peek();
                        for (int i = n - 1; i >= 0; i--)
                            Stack.Push(buffer[i]);
                        Stack.Push(xn);
                    }
                    break;
                case ScriptOp.OP_ROLL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int n = (int)(BigInteger)Stack.Pop();
                        if (n < 0) return VMState.FAULT;
                        if (n == 0) return VMState.NONE;
                        if (Stack.Count < n + 1) return VMState.FAULT;
                        StackItem[] buffer = new StackItem[n];
                        for (int i = 0; i < n; i++)
                            buffer[i] = Stack.Pop();
                        StackItem xn = Stack.Pop();
                        for (int i = n - 1; i >= 0; i--)
                            Stack.Push(buffer[i]);
                        Stack.Push(xn);
                    }
                    break;
                case ScriptOp.OP_ROT:
                    {
                        if (Stack.Count < 3) return VMState.FAULT;
                        StackItem x3 = Stack.Pop();
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        Stack.Push(x2);
                        Stack.Push(x3);
                        Stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_SWAP:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        Stack.Push(x2);
                        Stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_TUCK:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        Stack.Push(x2);
                        Stack.Push(x1);
                        Stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_CAT:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        byte[][] b1 = x1.GetBytesArray();
                        byte[][] b2 = x2.GetBytesArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        byte[][] r = b1.Zip(b2, (p1, p2) => p1.Concat(p2).ToArray()).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SUBSTR:
                    {
                        if (Stack.Count < 3) return VMState.FAULT;
                        int count = (int)(BigInteger)Stack.Pop();
                        if (count < 0) return VMState.FAULT;
                        int index = (int)(BigInteger)Stack.Pop();
                        if (index < 0) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] s = x.GetBytesArray();
                        s = s.Select(p => p.Skip(index).Take(count).ToArray()).ToArray();
                        if (x.IsArray)
                            Stack.Push(s);
                        else
                            Stack.Push(s[0]);
                    }
                    break;
                case ScriptOp.OP_LEFT:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int count = (int)(BigInteger)Stack.Pop();
                        if (count < 0) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] s = x.GetBytesArray();
                        s = s.Select(p => p.Take(count).ToArray()).ToArray();
                        if (x.IsArray)
                            Stack.Push(s);
                        else
                            Stack.Push(s[0]);
                    }
                    break;
                case ScriptOp.OP_RIGHT:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int count = (int)(BigInteger)Stack.Pop();
                        if (count < 0) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] s = x.GetBytesArray();
                        if (s.Any(p => p.Length < count)) return VMState.FAULT;
                        s = s.Select(p => p.Skip(p.Length - count).ToArray()).ToArray();
                        if (x.IsArray)
                            Stack.Push(s);
                        else
                            Stack.Push(s[0]);
                    }
                    break;
                case ScriptOp.OP_SIZE:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Peek();
                        int[] r = x.GetBytesArray().Select(p => p.Length).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;

                // Bitwise logic
                case ScriptOp.OP_INVERT:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => ~p).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_AND:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 & p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_OR:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 | p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_XOR:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 ^ p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_EQUAL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        byte[][] b1 = x1.GetBytesArray();
                        byte[][] b2 = x2.GetBytesArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1.SequenceEqual(p2)).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;

                // Numeric
                case ScriptOp.OP_1ADD:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p + BigInteger.One).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_1SUB:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p - BigInteger.One).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_2MUL:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p << 1).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_2DIV:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => p >> 1).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NEGATE:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => -p).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_ABS:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        BigInteger[] r = x.GetIntArray().Select(p => BigInteger.Abs(p)).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NOT:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        bool[] r = x.GetBooleanArray().Select(p => !p).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_0NOTEQUAL:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        bool[] r = x.GetIntArray().Select(p => p != BigInteger.Zero).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_ADD:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 + p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SUB:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 - p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MUL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 * p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_DIV:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 / p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MOD:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 % p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_LSHIFT:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 << (int)p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_RSHIFT:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => p1 >> (int)p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_BOOLAND:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        bool[] b1 = x1.GetBooleanArray();
                        bool[] b2 = x2.GetBooleanArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 && p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_BOOLOR:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        bool[] b1 = x1.GetBooleanArray();
                        bool[] b2 = x2.GetBooleanArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 || p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NUMEQUAL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 == p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_NUMNOTEQUAL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 != p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_LESSTHAN:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 < p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_GREATERTHAN:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 > p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_LESSTHANOREQUAL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 <= p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_GREATERTHANOREQUAL:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        bool[] r = b1.Zip(b2, (p1, p2) => p1 >= p2).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MIN:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Min(p1, p2)).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_MAX:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        BigInteger[] b1 = x1.GetIntArray();
                        BigInteger[] b2 = x2.GetIntArray();
                        if (b1.Length != b2.Length) return VMState.FAULT;
                        BigInteger[] r = b1.Zip(b2, (p1, p2) => BigInteger.Max(p1, p2)).ToArray();
                        if (x1.IsArray || x2.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_WITHIN:
                    {
                        if (Stack.Count < 3) return VMState.FAULT;
                        BigInteger b = (BigInteger)Stack.Pop();
                        BigInteger a = (BigInteger)Stack.Pop();
                        BigInteger x = (BigInteger)Stack.Pop();
                        Stack.Push(a <= x && x < b);
                    }
                    break;

                // Crypto
                case ScriptOp.OP_RIPEMD160:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.RIPEMD160()).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SHA1:
                    using (SHA1Managed sha = new SHA1Managed())
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => sha.ComputeHash(p)).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_SHA256:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.Sha256()).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_HASH160:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.Sha256().RIPEMD160()).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_HASH256:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem x = Stack.Pop();
                        byte[][] r = x.GetBytesArray().Select(p => p.Sha256().Sha256()).ToArray();
                        if (x.IsArray)
                            Stack.Push(r);
                        else
                            Stack.Push(r[0]);
                    }
                    break;
                case ScriptOp.OP_CHECKSIG:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        byte[] pubkey = (byte[])Stack.Pop();
                        byte[] signature = (byte[])Stack.Pop();
                        Stack.Push(VerifySignature(hash, signature, pubkey));
                    }
                    break;
                case ScriptOp.OP_CHECKMULTISIG:
                    {
                        if (Stack.Count < 4) return VMState.FAULT;
                        int n = (int)(BigInteger)Stack.Pop();
                        if (n < 1) return VMState.FAULT;
                        if (Stack.Count < n + 2) return VMState.FAULT;
                        nOpCount += n;
                        if (nOpCount > MAXSTEPS) return VMState.FAULT;
                        byte[][] pubkeys = new byte[n][];
                        for (int i = 0; i < n; i++)
                            pubkeys[i] = (byte[])Stack.Pop();
                        int m = (int)(BigInteger)Stack.Pop();
                        if (m < 1 || m > n) return VMState.FAULT;
                        if (Stack.Count < m) return VMState.FAULT;
                        byte[][] signatures = new byte[m][];
                        for (int i = 0; i < m; i++)
                            signatures[i] = (byte[])Stack.Pop();
                        bool fSuccess = true;
                        for (int i = 0, j = 0; fSuccess && i < m && j < n;)
                        {
                            if (VerifySignature(hash, signatures[i], pubkeys[j]))
                                i++;
                            j++;
                            if (m - i > n - j)
                                fSuccess = false;
                        }
                        Stack.Push(fSuccess);
                    }
                    break;

                // Array
                case ScriptOp.OP_ARRAYSIZE:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem arr = Stack.Pop();
                        if (arr.IsArray)
                            Stack.Push(arr.Count);
                        else
                            Stack.Push(1);
                    }
                    break;
                case ScriptOp.OP_PACK:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        int c = (int)(BigInteger)Stack.Pop();
                        if (Stack.Count < c) return VMState.FAULT;
                        StackItem[] arr = new StackItem[c];
                        while (c-- > 0)
                        {
                            arr[c] = Stack.Pop();
                            if (arr[c].IsArray) return VMState.FAULT;
                        }
                        Stack.Push(new StackItem(arr));
                    }
                    break;
                case ScriptOp.OP_UNPACK:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem arr = Stack.Pop();
                        if (!arr.IsArray) return VMState.FAULT;
                        foreach (StackItem item in arr)
                            Stack.Push(item);
                        Stack.Push(arr.Count);
                    }
                    break;
                case ScriptOp.OP_DISTINCT:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(new StackItem(Stack.Pop().Distinct()));
                    break;
                case ScriptOp.OP_SORT:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Pop().GetIntArray().OrderBy(p => p).ToArray());
                    break;
                case ScriptOp.OP_REVERSE:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(new StackItem(Stack.Pop().Reverse()));
                    break;
                case ScriptOp.OP_CONCAT:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        int c = (int)(BigInteger)Stack.Pop();
                        if (Stack.Count < c) return VMState.FAULT;
                        IEnumerable<StackItem> items = Enumerable.Empty<StackItem>();
                        while (c-- > 0)
                            items = Stack.Pop().Concat(items);
                        Stack.Push(new StackItem(items));
                    }
                    break;
                case ScriptOp.OP_UNION:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        int c = (int)(BigInteger)Stack.Pop();
                        if (Stack.Count < c) return VMState.FAULT;
                        IEnumerable<StackItem> items = Enumerable.Empty<StackItem>();
                        while (c-- > 0)
                            items = Stack.Pop().Union(items);
                        Stack.Push(new StackItem(items));
                    }
                    break;
                case ScriptOp.OP_INTERSECT:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        int c = (int)(BigInteger)Stack.Pop();
                        if (Stack.Count < c) return VMState.FAULT;
                        IEnumerable<StackItem> items = Enumerable.Empty<StackItem>();
                        while (c-- > 0)
                            items = Stack.Pop().Intersect(items);
                        Stack.Push(new StackItem(items));
                    }
                    break;
                case ScriptOp.OP_EXCEPT:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        StackItem x2 = Stack.Pop();
                        StackItem x1 = Stack.Pop();
                        Stack.Push(new StackItem(x1.Except(x2)));
                    }
                    break;
                case ScriptOp.OP_TAKE:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int count = (int)(BigInteger)Stack.Pop();
                        Stack.Push(new StackItem(Stack.Pop().Take(count)));
                    }
                    break;
                case ScriptOp.OP_SKIP:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int count = (int)(BigInteger)Stack.Pop();
                        Stack.Push(new StackItem(Stack.Pop().Skip(count)));
                    }
                    break;
                case ScriptOp.OP_PICKITEM:
                    {
                        if (Stack.Count < 2) return VMState.FAULT;
                        int index = (int)(BigInteger)Stack.Pop();
                        StackItem arr = Stack.Pop();
                        if (arr.Count <= index)
                            Stack.Push(new StackItem((byte[])null));
                        else
                            Stack.Push(arr[index]);
                    }
                    break;
                case ScriptOp.OP_ALL:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Pop().All(p => p));
                    break;
                case ScriptOp.OP_ANY:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Pop().Any(p => p));
                    break;
                case ScriptOp.OP_SUM:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Pop().Aggregate(BigInteger.Zero, (s, p) => s + (BigInteger)p));
                    break;
                case ScriptOp.OP_AVERAGE:
                    {
                        if (Stack.Count < 1) return VMState.FAULT;
                        StackItem arr = Stack.Pop();
                        if (arr.Count == 0) return VMState.FAULT;
                        Stack.Push(arr.Aggregate(BigInteger.Zero, (s, p) => s + (BigInteger)p, p => p / arr.Count));
                    }
                    break;
                case ScriptOp.OP_MAXITEM:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Pop().GetIntArray().Max());
                    break;
                case ScriptOp.OP_MINITEM:
                    if (Stack.Count < 1) return VMState.FAULT;
                    Stack.Push(Stack.Pop().GetIntArray().Min());
                    break;

                default:
                    return VMState.FAULT;
            }
            return VMState.NONE;
        }

        private bool ExecuteScript(byte[] script, bool push_only)
        {
            using (MemoryStream ms = new MemoryStream(script, false))
            using (BinaryReader opReader = new BinaryReader(ms))
            {
                try
                {
                    while (opReader.BaseStream.Position < script.Length)
                    {
                        ScriptOp opcode = (ScriptOp)opReader.ReadByte();
                        if (push_only && opcode > ScriptOp.OP_16) return false;
                        VMState state = ExecuteOp(opcode, opReader);
                        if (state.HasFlag(VMState.FAULT)) return false;
                        if (state.HasFlag(VMState.HALT)) return true;
                    }
                }
                catch (Exception ex) when (ex is EndOfStreamException || ex is FormatException || ex is InvalidCastException)
                {
                    return false;
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
