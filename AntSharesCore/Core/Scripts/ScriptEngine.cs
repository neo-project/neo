using AntShares.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace AntShares.Core.Scripts
{
    internal class ScriptEngine
    {
        private const int MAXSTEPS = 1200;

        private Script script;
        private byte[] hash;

        private Stack stack = new Stack();
        private Stack altStack = new Stack();
        private int nOpCount = 0;

        public ScriptEngine(Script script, byte[] hash)
        {
            this.script = script;
            this.hash = hash;
        }

        public bool Execute()
        {
            if (!ExecuteScript(script.StackScript, true)) return false;
            if (!ExecuteScript(script.RedeemScript, false)) return false;
            return stack.Count == 0 || (stack.Count == 1 && stack.PeekBool());
        }

        private bool ExecuteOp(ScriptOp opcode, BinaryReader opReader)
        {
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
                case ScriptOp.OP_VERIFY:
                    if (stack.Count < 1) return false;
                    if (stack.PeekBool())
                        stack.PopBytes();
                    else
                        return false;
                    break;
                case ScriptOp.OP_RETURN:
                    return false;

                // Stack ops
                case ScriptOp.OP_TOALTSTACK:
                    if (stack.Count < 1) return false;
                    altStack.Push(stack.PopBytes());
                    break;
                case ScriptOp.OP_FROMALTSTACK:
                    if (altStack.Count < 1) return false;
                    stack.Push(altStack.PopBytes());
                    break;
                case ScriptOp.OP_2DROP:
                    if (stack.Count < 2) return false;
                    stack.PopBytes();
                    stack.PopBytes();
                    break;
                case ScriptOp.OP_2DUP:
                    {
                        if (stack.Count < 2) return false;
                        byte[] x1 = stack.PopBytes();
                        byte[] x2 = stack.PeekBytes();
                        stack.Push(x1);
                        stack.Push(x2);
                        stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_3DUP:
                    {
                        if (stack.Count < 3) return false;
                        byte[] x1 = stack.PopBytes();
                        byte[] x2 = stack.PopBytes();
                        byte[] x3 = stack.PeekBytes();
                        stack.Push(x2);
                        stack.Push(x1);
                        stack.Push(x3);
                        stack.Push(x2);
                        stack.Push(x1);
                    }
                    break;
                case ScriptOp.OP_2SWAP:
                    {
                        if (stack.Count < 4) return false;
                        byte[] x1 = stack.PopBytes();
                        byte[] x2 = stack.PopBytes();
                        byte[] x3 = stack.PopBytes();
                        byte[] x4 = stack.PopBytes();
                        stack.Push(x2);
                        stack.Push(x1);
                        stack.Push(x4);
                        stack.Push(x3);
                    }
                    break;
                case ScriptOp.OP_IFDUP:
                    if (stack.Count < 1) return false;
                    if (stack.PeekBool())
                        stack.Push(stack.PeekBytes());
                    break;
                case ScriptOp.OP_DEPTH:
                    stack.Push(stack.Count);
                    break;
                case ScriptOp.OP_DROP:
                    if (stack.Count < 1) return false;
                    stack.PopBytes();
                    break;
                case ScriptOp.OP_DUP:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PeekBytes());
                    break;
                case ScriptOp.OP_SWAP:
                    {
                        if (stack.Count < 2) return false;
                        byte[] x1 = stack.PopBytes();
                        byte[] x2 = stack.PopBytes();
                        stack.Push(x1);
                        stack.Push(x2);
                    }
                    break;
                case ScriptOp.OP_SIZE:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PeekBytes().Length);
                    break;

                // Bitwise logic
                case ScriptOp.OP_EQUAL:
                case ScriptOp.OP_EQUALVERIFY:
                    if (stack.Count < 2) return false;
                    stack.Push(stack.PopBytes().SequenceEqual(stack.PopBytes()));
                    if (opcode == ScriptOp.OP_EQUALVERIFY)
                        return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    break;

                // Numeric
                case ScriptOp.OP_1ADD:
                    if (stack.Count < 1) return false;
                    stack.Push((int)stack.PopBigInteger() + 1);
                    break;
                case ScriptOp.OP_1SUB:
                    if (stack.Count < 1) return false;
                    stack.Push((int)stack.PopBigInteger() - 1);
                    break;
                case ScriptOp.OP_2MUL:
                    if (stack.Count < 1) return false;
                    stack.Push((int)stack.PopBigInteger() * 2);
                    break;
                case ScriptOp.OP_2DIV:
                    if (stack.Count < 1) return false;
                    stack.Push((int)stack.PopBigInteger() / 2);
                    break;
                case ScriptOp.OP_NEGATE:
                    if (stack.Count < 1) return false;
                    stack.Push(-stack.PopBigInteger());
                    break;
                case ScriptOp.OP_ABS:
                    if (stack.Count < 1) return false;
                    stack.Push(BigInteger.Abs(stack.PopBigInteger()));
                    break;
                case ScriptOp.OP_NOT:
                    if (stack.Count < 1) return false;
                    stack.Push(!stack.PopBool());
                    break;
                case ScriptOp.OP_0NOTEQUAL:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PopBigInteger() != 0);
                    break;
                case ScriptOp.OP_ADD:
                    if (stack.Count < 2) return false;
                    stack.Push((int)stack.PopBigInteger() + (int)stack.PopBigInteger());
                    break;
                case ScriptOp.OP_SUB:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a - b);
                    }
                    break;
                case ScriptOp.OP_MUL:
                    if (stack.Count < 2) return false;
                    stack.Push((int)stack.PopBigInteger() * (int)stack.PopBigInteger());
                    break;
                case ScriptOp.OP_DIV:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a / b);
                    }
                    break;
                case ScriptOp.OP_MOD:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a % b);
                    }
                    break;
                case ScriptOp.OP_BOOLAND:
                    if (stack.Count < 2) return false;
                    stack.Push(stack.PopBool() && stack.PopBool());
                    break;
                case ScriptOp.OP_BOOLOR:
                    if (stack.Count < 2) return false;
                    stack.Push(stack.PopBool() || stack.PopBool());
                    break;
                case ScriptOp.OP_NUMEQUAL:
                case ScriptOp.OP_NUMEQUALVERIFY:
                    if (stack.Count < 2) return false;
                    stack.Push(stack.PopBigInteger() == stack.PopBigInteger());
                    if (opcode == ScriptOp.OP_NUMEQUALVERIFY)
                        return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    break;
                case ScriptOp.OP_NUMNOTEQUAL:
                    if (stack.Count < 2) return false;
                    stack.Push(stack.PopBigInteger() != stack.PopBigInteger());
                    break;
                case ScriptOp.OP_LESSTHAN:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a < b);
                    }
                    break;
                case ScriptOp.OP_GREATERTHAN:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a > b);
                    }
                    break;
                case ScriptOp.OP_LESSTHANOREQUAL:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a <= b);
                    }
                    break;
                case ScriptOp.OP_GREATERTHANOREQUAL:
                    {
                        if (stack.Count < 2) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        stack.Push(a >= b);
                    }
                    break;
                case ScriptOp.OP_MIN:
                    if (stack.Count < 2) return false;
                    stack.Push(BigInteger.Min(stack.PopBigInteger(), stack.PopBigInteger()));
                    break;
                case ScriptOp.OP_MAX:
                    if (stack.Count < 2) return false;
                    stack.Push(BigInteger.Max(stack.PopBigInteger(), stack.PopBigInteger()));
                    break;
                case ScriptOp.OP_WITHIN:
                    {
                        if (stack.Count < 3) return false;
                        int b = (int)stack.PopBigInteger();
                        int a = (int)stack.PopBigInteger();
                        int x = (int)stack.PopBigInteger();
                        stack.Push(a <= x && x < b);
                    }
                    break;

                // Crypto
                case ScriptOp.OP_RIPEMD160:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PopBytes().RIPEMD160());
                    break;
                case ScriptOp.OP_SHA1:
                    if (stack.Count < 1) return false;
                    using (SHA1Managed sha = new SHA1Managed())
                    {
                        stack.Push(sha.ComputeHash(stack.PopBytes()));
                    }
                    break;
                case ScriptOp.OP_SHA256:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PopBytes().Sha256());
                    break;
                case ScriptOp.OP_HASH160:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PopBytes().Sha256().RIPEMD160());
                    break;
                case ScriptOp.OP_HASH256:
                    if (stack.Count < 1) return false;
                    stack.Push(stack.PopBytes().Sha256().Sha256());
                    break;
                case ScriptOp.OP_CHECKSIG:
                case ScriptOp.OP_CHECKSIGVERIFY:
                    {
                        if (stack.Count < 2) return false;
                        byte[] pubkey = stack.PopBytes();
                        byte[] signature = stack.PopBytes();
                        stack.Push(VerifySignature(hash, signature, pubkey));
                        if (opcode == ScriptOp.OP_CHECKSIGVERIFY)
                            return ExecuteOp(ScriptOp.OP_VERIFY, opReader);
                    }
                    break;
                case ScriptOp.OP_CHECKMULTISIG:
                case ScriptOp.OP_CHECKMULTISIGVERIFY:
                    {
                        if (stack.Count < 4) return false;
                        int n = (int)stack.PopBigInteger();
                        if (n < 1) return false;
                        if (stack.Count < n + 2) return false;
                        nOpCount += n;
                        if (nOpCount > MAXSTEPS) return false;
                        byte[][] pubkeys = new byte[n][];
                        for (int i = 0; i < n; i++)
                        {
                            pubkeys[i] = stack.PopBytes();
                        }
                        int m = (int)stack.PopBigInteger();
                        if (m < 1 || m > n) return false;
                        if (stack.Count < m) return false;
                        List<byte[]> signatures = new List<byte[]>();
                        while (stack.Count > 0)
                        {
                            byte[] signature = stack.PopBytes();
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

                case ScriptOp.OP_EVAL:
                    if (stack.Count < 1) return false;
                    if (!ExecuteScript(stack.PopBytes(), false))
                        return false;
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
                pubkey = Secp256r1Point.DecodePoint(pubkey).EncodePoint(false).Skip(1).ToArray();
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
