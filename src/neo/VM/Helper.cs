using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;
using Buffer = Neo.VM.Types.Buffer;

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
            sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
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
            sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
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
            sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
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
                case object[] data:
                    for (int i = data.Length - 1; i >= 0; i--)
                        sb.EmitPush(data[i]);
                    sb.EmitPush(data.Length);
                    sb.Emit(OpCode.PACK);
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
                    throw new ArgumentException(obj.GetType().FullName);
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

        public static JObject ToJson(this StackItem item)
        {
            return ToJson(item, null);
        }

        private static JObject ToJson(StackItem item, HashSet<StackItem> context)
        {
            JObject json = new JObject();
            json["type"] = item.Type;
            switch (item)
            {
                case Array array:
                    context ??= new HashSet<StackItem>(ReferenceEqualityComparer.Default);
                    if (!context.Add(array)) throw new InvalidOperationException();
                    json["value"] = new JArray(array.Select(p => ToJson(p, context)));
                    break;
                case Boolean boolean:
                    json["value"] = boolean.GetBoolean();
                    break;
                case Buffer _:
                case ByteString _:
                    json["value"] = Convert.ToBase64String(item.GetSpan());
                    break;
                case Integer integer:
                    json["value"] = integer.GetInteger().ToString();
                    break;
                case Map map:
                    context ??= new HashSet<StackItem>(ReferenceEqualityComparer.Default);
                    if (!context.Add(map)) throw new InvalidOperationException();
                    json["value"] = new JArray(map.Select(p =>
                    {
                        JObject item = new JObject();
                        item["key"] = ToJson(p.Key, context);
                        item["value"] = ToJson(p.Value, context);
                        return item;
                    }));
                    break;
                case Pointer pointer:
                    json["value"] = pointer.Position;
                    break;
            }
            return json;
        }

        public static ContractParameter ToParameter(this StackItem item)
        {
            return ToParameter(item, null);
        }

        private static ContractParameter ToParameter(StackItem item, List<(StackItem, ContractParameter)> context)
        {
            if (item is null) throw new ArgumentNullException();
            ContractParameter parameter = null;
            switch (item)
            {
                case Array array:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
                    else
                        (_, parameter) = context.FirstOrDefault(p => ReferenceEquals(p.Item1, item));
                    if (parameter is null)
                    {
                        parameter = new ContractParameter { Type = ContractParameterType.Array };
                        context.Add((item, parameter));
                        parameter.Value = array.Select(p => ToParameter(p, context)).ToList();
                    }
                    break;
                case Map map:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
                    else
                        (_, parameter) = context.FirstOrDefault(p => ReferenceEquals(p.Item1, item));
                    if (parameter is null)
                    {
                        parameter = new ContractParameter { Type = ContractParameterType.Map };
                        context.Add((item, parameter));
                        parameter.Value = map.Select(p => new KeyValuePair<ContractParameter, ContractParameter>(ToParameter(p.Key, context), ToParameter(p.Value, context))).ToList();
                    }
                    break;
                case Boolean _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Boolean,
                        Value = item.GetBoolean()
                    };
                    break;
                case ByteString array:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.ByteArray,
                        Value = array.GetSpan().ToArray()
                    };
                    break;
                case Integer i:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Integer,
                        Value = i.GetInteger()
                    };
                    break;
                case InteropInterface _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.InteropInterface
                    };
                    break;
                case Null _:
                    parameter = new ContractParameter
                    {
                        Type = ContractParameterType.Any
                    };
                    break;
                default:
                    throw new ArgumentException($"StackItemType({item.Type}) is not supported to ContractParameter.");
            }
            return parameter;
        }

        public static StackItem ToStackItem(this ContractParameter parameter)
        {
            return ToStackItem(parameter, null);
        }

        private static StackItem ToStackItem(ContractParameter parameter, List<(StackItem, ContractParameter)> context)
        {
            if (parameter is null) throw new ArgumentNullException();
            if (parameter.Value is null) return StackItem.Null;
            StackItem stackItem = null;
            switch (parameter.Type)
            {
                case ContractParameterType.Array:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
                    else
                        (stackItem, _) = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter));
                    if (stackItem is null)
                    {
                        stackItem = new Array(((IList<ContractParameter>)parameter.Value).Select(p => ToStackItem(p, context)));
                        context.Add((stackItem, parameter));
                    }
                    break;
                case ContractParameterType.Map:
                    if (context is null)
                        context = new List<(StackItem, ContractParameter)>();
                    else
                        (stackItem, _) = context.FirstOrDefault(p => ReferenceEquals(p.Item2, parameter));
                    if (stackItem is null)
                    {
                        Map map = new Map();
                        foreach (var pair in (IList<KeyValuePair<ContractParameter, ContractParameter>>)parameter.Value)
                            map[(PrimitiveType)ToStackItem(pair.Key, context)] = ToStackItem(pair.Value, context);
                        stackItem = map;
                        context.Add((stackItem, parameter));
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
                default:
                    throw new ArgumentException($"ContractParameterType({parameter.Type}) is not supported to StackItem.");
            }
            return stackItem;
        }
    }
}
