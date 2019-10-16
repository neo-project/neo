using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract
{
    public static class Helper
    {
        public static StackItem DeserializeStackItem(this byte[] data, uint maxArraySize, uint maxItemSize)
        {
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                return DeserializeStackItem(reader, maxArraySize, maxItemSize);
            }
        }

        private static StackItem DeserializeStackItem(BinaryReader reader, uint maxArraySize, uint maxItemSize)
        {
            Stack<StackItem> deserialized = new Stack<StackItem>();
            int undeserialized = 1;
            while (undeserialized-- > 0)
            {
                StackItemType type = (StackItemType)reader.ReadByte();
                switch (type)
                {
                    case StackItemType.ByteArray:
                        deserialized.Push(new ByteArray(reader.ReadVarBytes((int)maxItemSize)));
                        break;
                    case StackItemType.Boolean:
                        deserialized.Push(new VMBoolean(reader.ReadBoolean()));
                        break;
                    case StackItemType.Integer:
                        deserialized.Push(new Integer(new BigInteger(reader.ReadVarBytes(ExecutionEngine.MaxSizeForBigInteger))));
                        break;
                    case StackItemType.Array:
                    case StackItemType.Struct:
                        {
                            int count = (int)reader.ReadVarInt(maxArraySize);
                            deserialized.Push(new ContainerPlaceholder
                            {
                                Type = type,
                                ElementCount = count
                            });
                            undeserialized += count;
                        }
                        break;
                    case StackItemType.Map:
                        {
                            int count = (int)reader.ReadVarInt(maxArraySize);
                            deserialized.Push(new ContainerPlaceholder
                            {
                                Type = type,
                                ElementCount = count
                            });
                            undeserialized += count * 2;
                        }
                        break;
                    case StackItemType.Null:
                        deserialized.Push(StackItem.Null);
                        break;
                    default:
                        throw new FormatException();
                }
            }
            Stack<StackItem> stack_temp = new Stack<StackItem>();
            while (deserialized.Count > 0)
            {
                StackItem item = deserialized.Pop();
                if (item is ContainerPlaceholder placeholder)
                {
                    switch (placeholder.Type)
                    {
                        case StackItemType.Array:
                            VMArray array = new VMArray();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                array.Add(stack_temp.Pop());
                            item = array;
                            break;
                        case StackItemType.Struct:
                            Struct @struct = new Struct();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                                @struct.Add(stack_temp.Pop());
                            item = @struct;
                            break;
                        case StackItemType.Map:
                            Map map = new Map();
                            for (int i = 0; i < placeholder.ElementCount; i++)
                            {
                                StackItem key = stack_temp.Pop();
                                StackItem value = stack_temp.Pop();
                                map.Add(key, value);
                            }
                            item = map;
                            break;
                    }
                }
                stack_temp.Push(item);
            }
            return stack_temp.Peek();
        }

        public static bool IsMultiSigContract(this byte[] script, out int m, out int n)
        {
            m = 0; n = 0;
            int i = 0;
            if (script.Length < 41) return false;
            if (script[i] > (byte)OpCode.PUSH16) return false;
            if (script[i] < (byte)OpCode.PUSH1 && script[i] != 1 && script[i] != 2) return false;
            switch (script[i])
            {
                case 1:
                    m = script[++i];
                    ++i;
                    break;
                case 2:
                    m = script.ToUInt16(++i);
                    i += 2;
                    break;
                default:
                    m = script[i++] - 80;
                    break;
            }
            if (m < 1 || m > 1024) return false;
            while (script[i] == 33)
            {
                i += 34;
                if (script.Length <= i) return false;
                ++n;
            }
            if (n < m || n > 1024) return false;
            switch (script[i])
            {
                case 1:
                    if (n != script[++i]) return false;
                    ++i;
                    break;
                case 2:
                    if (script.Length < i + 3 || n != script.ToUInt16(++i)) return false;
                    i += 2;
                    break;
                default:
                    if (n != script[i++] - 80) return false;
                    break;
            }
            if (script[i++] != (byte)OpCode.SYSCALL) return false;
            if (script.Length != i + 4) return false;
            if (BitConverter.ToUInt32(script, i) != InteropService.Neo_Crypto_CheckMultiSig)
                return false;
            return true;
        }

        public static bool IsSignatureContract(this byte[] script)
        {
            if (script.Length != 39) return false;
            if (script[0] != (byte)OpCode.PUSHBYTES33
                || script[34] != (byte)OpCode.SYSCALL
                || BitConverter.ToUInt32(script, 35) != InteropService.Neo_Crypto_CheckSig)
                return false;
            return true;
        }

        public static bool IsStandardContract(this byte[] script)
        {
            return script.IsSignatureContract() || script.IsMultiSigContract(out _, out _);
        }

        public static byte[] Serialize(this StackItem item)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                SerializeStackItem(item, writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        private static void SerializeStackItem(StackItem item, BinaryWriter writer)
        {
            List<StackItem> serialized = new List<StackItem>();
            Stack<StackItem> unserialized = new Stack<StackItem>();
            unserialized.Push(item);
            while (unserialized.Count > 0)
            {
                item = unserialized.Pop();
                switch (item)
                {
                    case ByteArray _:
                        writer.Write((byte)StackItemType.ByteArray);
                        writer.WriteVarBytes(item.GetByteArray());
                        break;
                    case VMBoolean _:
                        writer.Write((byte)StackItemType.Boolean);
                        writer.Write(item.GetBoolean());
                        break;
                    case Integer _:
                        writer.Write((byte)StackItemType.Integer);
                        writer.WriteVarBytes(item.GetByteArray());
                        break;
                    case InteropInterface _:
                        throw new NotSupportedException();
                    case VMArray array:
                        if (serialized.Any(p => ReferenceEquals(p, array)))
                            throw new NotSupportedException();
                        serialized.Add(array);
                        if (array is Struct)
                            writer.Write((byte)StackItemType.Struct);
                        else
                            writer.Write((byte)StackItemType.Array);
                        writer.WriteVarInt(array.Count);
                        for (int i = array.Count - 1; i >= 0; i--)
                            unserialized.Push(array[i]);
                        break;
                    case Map map:
                        if (serialized.Any(p => ReferenceEquals(p, map)))
                            throw new NotSupportedException();
                        serialized.Add(map);
                        writer.Write((byte)StackItemType.Map);
                        writer.WriteVarInt(map.Count);
                        foreach (var pair in map.Reverse())
                        {
                            unserialized.Push(pair.Value);
                            unserialized.Push(pair.Key);
                        }
                        break;
                    case Null _:
                        writer.Write((byte)StackItemType.Null);
                        break;
                }
            }
        }

        public static uint ToInteropMethodHash(this string method)
        {
            return BitConverter.ToUInt32(Encoding.ASCII.GetBytes(method).Sha256(), 0);
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            return new UInt160(Crypto.Default.Hash160(script));
        }

        internal static bool VerifyWitnesses(this IVerifiable verifiable, Snapshot snapshot, long gas)
        {
            if (gas < 0) return false;

            UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying(snapshot);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (hashes.Length != verifiable.Witnesses.Length) return false;
            for (int i = 0; i < hashes.Length; i++)
            {
                byte[] verification = verifiable.Witnesses[i].VerificationScript;
                if (verification.Length == 0)
                {
                    verification = snapshot.Contracts.TryGet(hashes[i])?.Script;
                    if (verification is null) return false;
                }
                else
                {
                    if (hashes[i] != verifiable.Witnesses[i].ScriptHash) return false;
                }
                using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, verifiable, snapshot, gas))
                {
                    engine.LoadScript(verification);
                    engine.LoadScript(verifiable.Witnesses[i].InvocationScript);
                    if (engine.Execute().HasFlag(VMState.FAULT)) return false;
                    if (engine.ResultStack.Count != 1 || !engine.ResultStack.Pop().GetBoolean()) return false;
                }
            }
            return true;
        }
    }
}
