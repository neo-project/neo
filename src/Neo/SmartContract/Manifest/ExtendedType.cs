// Copyright (C) 2015-2025 The Neo Project.
//
// ExtendedType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Manifest
{
#nullable enable

    public class ExtendedType : IInteroperable, IEquatable<ExtendedType>
    {
        /// <summary>
        /// The type of the parameter. It can be any value of <see cref="ContractParameterType"/> except <see cref="ContractParameterType.Void"/>.
        /// </summary>
        public ContractParameterType Type { get; set; }

        /// <summary>
        /// NamedType is used to refer to one of the types defined in the namedtypes object of Contract,
        /// so namedtypes MUST contain a field named name.
        /// This field is only used for structures (ordered set of named values of diffent types),
        /// if used other fields MUST NOT be set, except for the type which MUST be an Array.
        /// Value string MUST start with a letter and can contain alphanumeric characters and dots.
        /// It MUST NOT be longer than 64 characters.
        /// </summary>
        public string? NamedType { get; set; }

        /// <summary>
        /// length is an optional field that can be used for Integer, ByteArray, String or Array types and MUST NOT be used for other types.
        /// When used it specifies the maximum possible byte length of an integer/byte array/string or number of array elements.
        /// Any of these lengths MUST NOT exceed NeoVM limitations that are relevant for the current version of it.
        /// Length 0 means "unlimited".
        /// </summary>
        public int? Length { get; set; }

        /// <summary>
        /// forbidnull is an optional field that can be used for Hash160, Hash256,
        /// ByteArray, String, Array, Map or InteropInterface types and MUST NOT be used for other types.
        /// It allows to specify that the method accepts or event emits only non-null values for this field.
        /// The default (if not specified) is "false", meaning that null can be used.
        /// </summary>
        public bool? ForbidNull { get; set; }

        /// <summary>
        /// interface is only used in conjuction with the InteropInterface type and MUST NOT be used for other types,
        /// when used it specifies which interop interface is used.
        /// The only valid defined value for it is "IIterator" which means an iterator object.
        /// When used it MUST be accompanied with the value object that specifies the type of each individual element returned from the iterator.
        /// </summary>
        public Nep25Interface? Interface { get; set; }

        /// <summary>
        /// key is only used along with the Map type (MUST NOT be used for other types) and can have Signature, Boolean, Integer,
        /// Hash160, Hash256, ByteArray, PublicKey or String value, that is all the basic types that can be used as a map key.
        /// </summary>
        public Nep25Key? Key { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            FromStackItem((VM.Types.Array)stackItem, 0);
        }

        internal void FromStackItem(VM.Types.Array @struct, int startIndex)
        {
            Type = (ContractParameterType)(byte)@struct[startIndex++].GetInteger();
            if (!Enum.IsDefined(typeof(ContractParameterType), Type)) throw new FormatException();
            NamedType = @struct[startIndex++].GetString();

            if (@struct[startIndex++] is Integer length)
            {
                Length = checked((int)length.GetInteger());
                if (Length < 0) throw new FormatException("Length must be non-negative.");
                if (Length > ExecutionEngineLimits.Default.MaxItemSize) throw new FormatException($"Length must less than {ExecutionEngineLimits.Default.MaxItemSize}.");
            }
            else
            {
                Length = null;
            }

            if (@struct[startIndex++] is VM.Types.Boolean forbidnull)
            {
                ForbidNull = forbidnull.GetBoolean();
            }
            else
            {
                ForbidNull = null;
            }

            if (@struct[startIndex++] is ByteString interf)
            {
                if (!Enum.TryParse<Nep25Interface>(interf.GetString(), false, out var inferValue))
                    throw new FormatException();

                Interface = inferValue;
            }
            else
            {
                Interface = null;
            }

            if (@struct[startIndex++] is ByteString key)
            {
                if (!Enum.TryParse<Nep25Key>(key.GetString(), false, out var keyValue))
                    throw new FormatException();

                Key = keyValue;
            }
            else
            {
                Key = null;
            }
        }

        internal StackItem ToStackItem(IReferenceCounter referenceCounter, Struct @struct)
        {
            @struct.Add((byte)Type);
            @struct.Add(NamedType ?? StackItem.Null);
            @struct.Add(Length ?? StackItem.Null);
            @struct.Add(ForbidNull ?? StackItem.Null);
            @struct.Add(Interface?.ToString() ?? StackItem.Null);
            @struct.Add(Key?.ToString() ?? StackItem.Null);
            return @struct;
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter referenceCounter)
        {
            var @struct = new Struct(referenceCounter);
            ToStackItem(referenceCounter, @struct);
            return @struct;
        }

        /// <summary>
        /// Converts the type from a JSON object.
        /// </summary>
        /// <param name="json">The method represented by a JSON object.</param>
        /// <returns>The extended type.</returns>
        public static ExtendedType FromJson(JObject json)
        {
            ExtendedType type = new()
            {
                Type = Enum.Parse<ContractParameterType>(json["type"]?.GetString() ?? throw new FormatException()),
                NamedType = json["namedtype"]?.GetString(),
            };
            if (!Enum.IsDefined(typeof(ContractParameterType), type.Type)) throw new FormatException();
            if (json["length"] != null)
            {
                type.Length = json["length"]!.GetInt32();
                if (type.Length < 0) throw new FormatException("Length must be non-negative.");
                if (type.Length > ExecutionEngineLimits.Default.MaxItemSize) throw new FormatException($"Length must less than {ExecutionEngineLimits.Default.MaxItemSize}.");
            }
            if (json["forbidnull"] != null)
            {
                type.ForbidNull = json["forbidnull"]!.GetBoolean();
            }
            if (json["interface"] != null)
            {
                if (!Enum.TryParse<Nep25Interface>(json["interface"]!.GetString(), true, out var interfaceValue))
                    throw new FormatException("Invalid interface value.");
                type.Interface = interfaceValue;
            }
            if (json["key"] != null)
            {
                if (!Enum.TryParse<Nep25Key>(json["key"]!.GetString(), true, out var keyValue))
                    throw new FormatException("Invalid key value.");
                type.Key = keyValue;
            }
            return type;
        }

        /// <summary>
        /// Converts the parameter to a JSON object.
        /// </summary>
        /// <returns>The parameter represented by a JSON object.</returns>
        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["type"] = Type.ToString();
            json["namedtype"] = NamedType;
            if (Length.HasValue)
            {
                json["length"] = Length.Value;
            }
            if (ForbidNull.HasValue)
            {
                json["forbidnull"] = ForbidNull.Value;
            }
            if (Interface.HasValue)
            {
                json["interface"] = Interface.Value.ToString();
            }
            if (Key.HasValue)
            {
                json["key"] = Key.Value.ToString();
            }
            return json;
        }

        public override bool Equals(object? obj) => Equals(obj as ExtendedType);

        public override int GetHashCode() => HashCode.Combine(Type, NamedType, Length, ForbidNull, Interface, Key);

        public bool Equals(ExtendedType? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Type == other.Type
                && NamedType == other.NamedType
                && Length == other.Length
                && ForbidNull == other.ForbidNull
                && Interface == other.Interface
                && Key == other.Key;
        }
    }
#nullable disable
}

