// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    internal class ContractMethodMetadata
    {
        public string Name { get; }
        public MethodInfo Handler { get; }
        public InteropParameterDescriptor[] Parameters { get; }
        public bool NeedApplicationEngine { get; }
        public bool NeedSnapshot { get; }
        public long CpuFee { get; }
        public long StorageFee { get; }
        public CallFlags RequiredCallFlags { get; }
        public ContractMethodDescriptor Descriptor { get; }

        public ContractMethodMetadata(MemberInfo member, ContractMethodAttribute attribute)
        {
            this.Name = attribute.Name ?? member.Name.ToLower()[0] + member.Name[1..];
            this.Handler = member switch
            {
                MethodInfo m => m,
                PropertyInfo p => p.GetMethod,
                _ => throw new ArgumentException(null, nameof(member))
            };
            ParameterInfo[] parameterInfos = this.Handler.GetParameters();
            if (parameterInfos.Length > 0)
            {
                NeedApplicationEngine = parameterInfos[0].ParameterType.IsAssignableFrom(typeof(ApplicationEngine));
                NeedSnapshot = parameterInfos[0].ParameterType.IsAssignableFrom(typeof(DataCache));
            }
            if (NeedApplicationEngine || NeedSnapshot)
                this.Parameters = parameterInfos.Skip(1).Select(p => new InteropParameterDescriptor(p)).ToArray();
            else
                this.Parameters = parameterInfos.Select(p => new InteropParameterDescriptor(p)).ToArray();
            this.CpuFee = attribute.CpuFee;
            this.StorageFee = attribute.StorageFee;
            this.RequiredCallFlags = attribute.RequiredCallFlags;
            this.Descriptor = new ContractMethodDescriptor
            {
                Name = Name,
                ReturnType = ToParameterType(Handler.ReturnType),
                Parameters = Parameters.Select(p => new ContractParameterDefinition { Type = ToParameterType(p.Type), Name = p.Name }).ToArray(),
                Safe = (attribute.RequiredCallFlags & ~CallFlags.ReadOnly) == 0,
                NonReentry = (attribute.RequiredCallFlags & CallFlags.NonReentry) != 0,
            };
        }

        private static ContractParameterType ToParameterType(Type type)
        {
            if (type.BaseType == typeof(ContractTask)) return ToParameterType(type.GenericTypeArguments[0]);
            if (type == typeof(ContractTask)) return ContractParameterType.Void;
            if (type == typeof(void)) return ContractParameterType.Void;
            if (type == typeof(bool)) return ContractParameterType.Boolean;
            if (type == typeof(sbyte)) return ContractParameterType.Integer;
            if (type == typeof(byte)) return ContractParameterType.Integer;
            if (type == typeof(short)) return ContractParameterType.Integer;
            if (type == typeof(ushort)) return ContractParameterType.Integer;
            if (type == typeof(int)) return ContractParameterType.Integer;
            if (type == typeof(uint)) return ContractParameterType.Integer;
            if (type == typeof(long)) return ContractParameterType.Integer;
            if (type == typeof(ulong)) return ContractParameterType.Integer;
            if (type == typeof(BigInteger)) return ContractParameterType.Integer;
            if (type == typeof(byte[])) return ContractParameterType.ByteArray;
            if (type == typeof(string)) return ContractParameterType.String;
            if (type == typeof(UInt160)) return ContractParameterType.Hash160;
            if (type == typeof(UInt256)) return ContractParameterType.Hash256;
            if (type == typeof(ECPoint)) return ContractParameterType.PublicKey;
            if (type == typeof(VM.Types.Boolean)) return ContractParameterType.Boolean;
            if (type == typeof(Integer)) return ContractParameterType.Integer;
            if (type == typeof(ByteString)) return ContractParameterType.ByteArray;
            if (type == typeof(VM.Types.Buffer)) return ContractParameterType.ByteArray;
            if (type == typeof(Array)) return ContractParameterType.Array;
            if (type == typeof(Struct)) return ContractParameterType.Array;
            if (type == typeof(Map)) return ContractParameterType.Map;
            if (type == typeof(StackItem)) return ContractParameterType.Any;
            if (type == typeof(object)) return ContractParameterType.Any;
            if (typeof(IInteroperable).IsAssignableFrom(type)) return ContractParameterType.Array;
            if (typeof(ISerializable).IsAssignableFrom(type)) return ContractParameterType.ByteArray;
            if (type.IsArray) return ContractParameterType.Array;
            if (type.IsEnum) return ContractParameterType.Integer;
            if (type.IsValueType) return ContractParameterType.Array;
            return ContractParameterType.InteropInterface;
        }
    }
}
