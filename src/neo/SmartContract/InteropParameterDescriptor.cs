using Neo.Cryptography.ECC;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace Neo.SmartContract
{
    internal class InteropParameterDescriptor
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
            [typeof(VM.Types.Array)] = p => p,
            [typeof(InteropInterface)] = p => p,
            [typeof(sbyte)] = p => (sbyte)p.GetBigInteger(),
            [typeof(byte)] = p => (byte)p.GetBigInteger(),
            [typeof(short)] = p => (short)p.GetBigInteger(),
            [typeof(ushort)] = p => (ushort)p.GetBigInteger(),
            [typeof(int)] = p => (int)p.GetBigInteger(),
            [typeof(uint)] = p => (uint)p.GetBigInteger(),
            [typeof(long)] = p => (long)p.GetBigInteger(),
            [typeof(ulong)] = p => (ulong)p.GetBigInteger(),
            [typeof(BigInteger)] = p => p.GetBigInteger(),
            [typeof(byte[])] = p => p.IsNull ? null : p.GetSpan().ToArray(),
            [typeof(string)] = p => p.IsNull ? null : p.GetString(),
            [typeof(UInt160)] = p => p.IsNull ? null : new UInt160(p.GetSpan()),
            [typeof(UInt256)] = p => p.IsNull ? null : new UInt256(p.GetSpan()),
            [typeof(ECPoint)] = p => p.IsNull ? null : ECPoint.DecodePoint(p.GetSpan(), ECCurve.Secp256r1),
        };

        public InteropParameterDescriptor(ParameterInfo parameterInfo)
            : this(parameterInfo.ParameterType)
        {
            this.Name = parameterInfo.Name;
        }

        public InteropParameterDescriptor(Type type)
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
