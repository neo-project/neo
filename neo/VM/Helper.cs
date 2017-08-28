using Neo.IO;
using System;
using System.Numerics;

namespace Neo.VM
{
    public static class Helper
    {
        public static ScriptBuilder Emit(this ScriptBuilder sb, params OpCode[] ops)
        {
            foreach (OpCode op in ops)
                sb.Emit(op);
            return sb;
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, bool useTailCall = false)
        {
            return sb.EmitAppCall(scriptHash.ToArray(), useTailCall);
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation, params object[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                EmitPush(sb, args[i]);
            sb.EmitPush(args.Length);
            sb.Emit(OpCode.PACK);
            sb.EmitPush(operation);
            sb.EmitAppCall(scriptHash);
            return sb;
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, ISerializable data)
        {
            return sb.EmitPush(data.ToArray());
        }

        private static void EmitPush(ScriptBuilder sb, object obj)
        {
            switch (obj)
            {
                case bool data:
                    sb.EmitPush(data);
                    break;
                case byte[] data:
                    sb.EmitPush(data);
                    break;
                case string data:
                    sb.EmitPush(data);
                    break;
                case BigInteger data:
                    sb.EmitPush(data);
                    break;
                case ISerializable data:
                    sb.EmitPush(data);
                    break;
                case sbyte data:
                    sb.EmitPush(data);
                    break;
                case byte data:
                    sb.EmitPush(data);
                    break;
                case short data:
                    sb.EmitPush(data);
                    break;
                case ushort data:
                    sb.EmitPush(data);
                    break;
                case int data:
                    sb.EmitPush(data);
                    break;
                case uint data:
                    sb.EmitPush(data);
                    break;
                case long data:
                    sb.EmitPush(data);
                    break;
                case ulong data:
                    sb.EmitPush(data);
                    break;
                case Enum data:
                    sb.EmitPush(BigInteger.Parse(data.ToString("d")));
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        public static ScriptBuilder EmitSysCall(this ScriptBuilder sb, string api, params object[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                EmitPush(sb, args[i]);
            return sb.EmitSysCall(api);
        }
    }
}
