using Neo.Cryptography.ECC;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace Neo.SmartContract
{
    public class InteropParameterDescriptor
    {
        public string Name { get; }
        public Type Type { get; }
        public Func<StackItem, object> Converter { get; }
        public bool IsEnum => Type.IsEnum;
        public bool IsArray => Type.IsArray && Type.GetElementType() != typeof(byte);
        public bool IsInterface { get; }

        private static readonly Dictionary<Type, Func<StackItem, object>> converters = new Dictionary<Type, Func<StackItem, object>>
        {
            [typeof(StackItem)] = p => p,
            [typeof(VM.Types.Pointer)] = p => p,
            [typeof(VM.Types.Array)] = p => p,
            [typeof(InteropInterface)] = p => p,
            [typeof(bool)] = p => p.GetBoolean(),
            [typeof(sbyte)] = p => (sbyte)p.GetInteger(),
            [typeof(byte)] = p => (byte)p.GetInteger(),
            [typeof(short)] = p => (short)p.GetInteger(),
            [typeof(ushort)] = p => (ushort)p.GetInteger(),
            [typeof(int)] = p => (int)p.GetInteger(),
            [typeof(uint)] = p => (uint)p.GetInteger(),
            [typeof(long)] = p => (long)p.GetInteger(),
            [typeof(ulong)] = p => (ulong)p.GetInteger(),
            [typeof(BigInteger)] = p => p.GetInteger(),
            [typeof(byte[])] = p => p.IsNull ? null : p.GetSpan().ToArray(),
            [typeof(string)] = p => p.IsNull ? null : p.GetString(),
            [typeof(UInt160)] = p => p.IsNull ? null : new UInt160(p.GetSpan()),
            [typeof(UInt256)] = p => p.IsNull ? null : new UInt256(p.GetSpan()),
            [typeof(ECPoint)] = p => p.IsNull ? null : ECPoint.DecodePoint(p.GetSpan(), ECCurve.Secp256r1),
        };

        internal InteropParameterDescriptor(ParameterInfo parameterInfo)
            : this(parameterInfo.ParameterType)
        {
            this.Name = parameterInfo.Name;
        }

        internal InteropParameterDescriptor(Type type)
        {
            this.Type = type;
            if (IsEnum)
            {
                Converter = converters[type.GetEnumUnderlyingType()];
            }
            else if (IsArray)
            {
                Converter = converters[type.GetElementType()];
            }
            else
            {
                IsInterface = !converters.TryGetValue(type, out var converter);
                if (IsInterface)
                    Converter = converters[typeof(InteropInterface)];
                else
                    Converter = converter;
            }
        }
    }
}
