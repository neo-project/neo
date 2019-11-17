using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;

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

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation)
        {
            sb.EmitPush(0);
            sb.Emit(OpCode.NEWARRAY);
            sb.EmitPush(operation);
            sb.EmitPush(scriptHash);
            sb.EmitSysCall(InteropService.System_Contract_Call);
            return sb;
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation, params ContractParameter[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                sb.EmitPush(args[i]);
            sb.EmitPush(args.Length);
            sb.Emit(OpCode.PACK);
            sb.EmitPush(operation);
            sb.EmitPush(scriptHash);
            sb.EmitSysCall(InteropService.System_Contract_Call);
            return sb;
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation, params object[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                sb.EmitPush(args[i]);
            sb.EmitPush(args.Length);
            sb.Emit(OpCode.PACK);
            sb.EmitPush(operation);
            sb.EmitPush(scriptHash);
            sb.EmitSysCall(InteropService.System_Contract_Call);
            return sb;
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, ISerializable data)
        {
            return sb.EmitPush(data.ToArray());
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, ContractParameter parameter)
        {
            switch (parameter.Type)
            {
                case ContractParameterType.Signature:
                case ContractParameterType.ByteArray:
                    sb.EmitPush((byte[])parameter.Value);
                    break;
                case ContractParameterType.Boolean:
                    sb.EmitPush((bool)parameter.Value);
                    break;
                case ContractParameterType.Integer:
                    if (parameter.Value is BigInteger bi)
                        sb.EmitPush(bi);
                    else
                        sb.EmitPush((BigInteger)typeof(BigInteger).GetConstructor(new[] { parameter.Value.GetType() }).Invoke(new[] { parameter.Value }));
                    break;
                case ContractParameterType.Hash160:
                    sb.EmitPush((UInt160)parameter.Value);
                    break;
                case ContractParameterType.Hash256:
                    sb.EmitPush((UInt256)parameter.Value);
                    break;
                case ContractParameterType.PublicKey:
                    sb.EmitPush((ECPoint)parameter.Value);
                    break;
                case ContractParameterType.String:
                    sb.EmitPush((string)parameter.Value);
                    break;
                case ContractParameterType.Array:
                    {
                        IList<ContractParameter> parameters = (IList<ContractParameter>)parameter.Value;
                        for (int i = parameters.Count - 1; i >= 0; i--)
                            sb.EmitPush(parameters[i]);
                        sb.EmitPush(parameters.Count);
                        sb.Emit(OpCode.PACK);
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
            return sb;
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, object obj)
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
                case null:
                    sb.Emit(OpCode.PUSHNULL);
                    break;
                default:
                    throw new ArgumentException();
            }
            return sb;
        }

        public static ScriptBuilder EmitSysCall(this ScriptBuilder sb, uint method, params object[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                EmitPush(sb, args[i]);
            return sb.EmitSysCall(method);
        }

        /// <summary>
        /// Generate scripts to call a specific method from a specific contract.
        /// </summary>
        /// <param name="scriptHash">contract script hash</param>
        /// <param name="operation">contract operation</param>
        /// <param name="args">operation arguments</param>
        /// <returns></returns>
        public static byte[] MakeScript(this UInt160 scriptHash, string operation, params object[] args)
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                if (args.Length > 0)
                    sb.EmitAppCall(scriptHash, operation, args);
                else
                    sb.EmitAppCall(scriptHash, operation);
                return sb.ToArray();
            }
        }

        public static ContractParameter ToParameter(this StackItem item)
        {
            return ToParameter(item, null);
        }

        private static ContractParameter ToParameter(StackItem item, List<Tuple<StackItem, ContractParameter>> context)
        {
            ContractParameter parameter = null;
            switch (item)
            {
                case VMArray array:
                    if (context is null)
                        context = new List<Tuple<StackItem, ContractParameter>>();
                    else
                        parameter = context.FirstOrDefault(p => ReferenceEquals(p.Item1, item))?.Item2;
                    if (parameter is null)
                    {
                        parameter = new ContractParameter { Type = ContractParameterType.Array };
                        context.Add(new Tuple<StackItem, ContractParameter>(item, parameter));
                        parameter.Value = array.Select(p => ToParameter(p, context)).ToList();
                    }
                    break;
                case Map map:
                    if (context is null)
                        context = new List<Tuple<StackItem, ContractParameter>>();
                    else
                        parameter = context.FirstOrDefault(p => ReferenceEquals(p.Item1, item))?.Item2;
                    if (parameter is null)
                    {
                        parameter = new ContractParameter { Type = ContractParameterType.Map };
                        context.Add(new Tuple<StackItem, ContractParameter>(item, parameter));
                        parameter.Value = map.Select(p => new KeyValuePair<ContractParameter, ContractParameter>(ToParameter(p.Key, context), ToParameter(p.Value, context))).ToList();
                    }
                    break;
                case VMBoolean _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Boolean,
                        Value = item.GetBoolean()
                    };
                    break;
                case ByteArray _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.ByteArray,
                        Value = item.GetByteArray()
                    };
                    break;
                case Integer _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Integer,
                        Value = item.GetBigInteger()
                    };
                    break;
                case InteropInterface _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.InteropInterface
                    };
                    break;
                default: // Null included
                    throw new ArgumentException();
            }
            return parameter;
        }

        public static StackItem ToStackItem(this ContractParameter parameter)
        {
            return ToStackItem(parameter, null);
        }

        private static StackItem ToStackItem(ContractParameter parameter, List<Tuple<StackItem, ContractParameter>> context)
        {
            StackItem stackItem = null;
            switch (parameter.Type)
            {
                case ContractParameterType.Array:
                    if (context is null)
                        context = new List<Tuple<StackItem, ContractParameter>>();
                    else
                        stackItem = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter))?.Item1;
                    if (stackItem is null)
                    {
                        stackItem = ((IList<ContractParameter>)parameter.Value).Select(p => ToStackItem(p, context)).ToList();
                        context.Add(new Tuple<StackItem, ContractParameter>(stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Map:
                    if (context is null)
                        context = new List<Tuple<StackItem, ContractParameter>>();
                    else
                        stackItem = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter))?.Item1;
                    if (stackItem is null)
                    {
                        stackItem = new Map(((IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value).ToDictionary(p => ToStackItem(p.Key, context), p => ToStackItem(p.Value, context)));
                        context.Add(new Tuple<StackItem, ContractParameter>(stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Boolean:
                    stackItem = (bool)parameter.Value;
                    break;
                case ContractParameterType.ByteArray:
                case ContractParameterType.Signature:
                    stackItem = (byte[])parameter.Value;
                    break;
                case ContractParameterType.Integer:
                    stackItem = (BigInteger)parameter.Value;
                    break;
                case ContractParameterType.Hash160:
                    stackItem = ((UInt160)parameter.Value).ToArray();
                    break;
                case ContractParameterType.Hash256:
                    stackItem = ((UInt256)parameter.Value).ToArray();
                    break;
                case ContractParameterType.PublicKey:
                    stackItem = ((ECPoint)parameter.Value).EncodePoint(true);
                    break;
                case ContractParameterType.String:
                    stackItem = (string)parameter.Value;
                    break;
                case ContractParameterType.InteropInterface:
                    break;
                default:
                    throw new ArgumentException($"ContractParameterType({parameter.Type}) is not supported to StackItem.");
            }
            return stackItem;
        }
    }
}
