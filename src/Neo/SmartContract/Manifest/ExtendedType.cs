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
using System.Diagnostics.CodeAnalysis;

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
        /// This field is only used for structures (ordered set of named values of different types),
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
        /// interface is only used in conjunction with the InteropInterface type and MUST NOT be used for other types,
        /// when used it specifies which interop interface is used.
        /// The only valid defined value for it is "IIterator" which means an iterator object.
        /// When used it MUST be accompanied with the value object that specifies the type of each individual element returned from the iterator.
        /// </summary>
        public Nep25Interface? Interface { get; set; }

        /// <summary>
        /// key is only used along with the Map type (MUST NOT be used for other types) and can have Signature, Boolean, Integer,
        /// Hash160, Hash256, ByteArray, PublicKey or String value, that is all the basic types that can be used as a map key.
        /// </summary>
        public ContractParameterType? Key { get; set; }

        /// <summary>
        /// value is used for Array, InteropInterface and Map types (type field) and MUST NOT be used with other base types.
        /// When used for Array it contains the type of an individual element of an array (ordered set of values of one type).
        /// If used for InteropInterface it contains the type of an individual iterator's value. If used for Map it contains map value type.
        /// If this field is used, fields MUST NOT be present.
        /// </summary>
        public ExtendedType? Value { get; set; }

        /// <summary>
        /// fields is used for Array type and when used it means that the type is a structure (ordered set of named values of diffent types),
        /// which has its fields defined directly here (unlike namedtype which is just a reference to namedtypes).
        /// It's an array with each member being a Parameter. fields MUST NOT be used in method parameter or return value definitions
        /// (these MUST use namedtype referring to a valid type specified in the namedtypes object).
        /// </summary>
        public ContractParameterDefinition[]? Fields { get; set; }

        public void FromStackItem(StackItem stackItem)
        {
            if (stackItem is not Map map) throw new FormatException("Map type was expected");

            if (!map.TryGetValue("type", out var type)) throw new FormatException("Incorrect Type");

            Type = (ContractParameterType)(byte)type.GetInteger();
            if (!Enum.IsDefined(typeof(ContractParameterType), Type)) throw new FormatException("Incorrect Type");
            if (Type == ContractParameterType.Void) throw new FormatException("Void Type is not allowed in NEP-25");

            if (map.TryGetValue("namedtype", out var val))
            {
                NamedType = val.GetString();
            }

            if (map.TryGetValue("length", out val))
            {
                if (val is Integer length)
                    Length = checked((int)length.GetInteger());
                else
                {
                    Length = null;
                    if (val is not null) throw new FormatException("Length must be Integer or null");
                }
            }

            if (map.TryGetValue("forbidnull", out val))
            {
                if (val is VM.Types.Boolean forbidnull)
                    ForbidNull = forbidnull.GetBoolean();
                else
                {
                    ForbidNull = null;
                    if (val is not null) throw new FormatException("ForbidNull must be Boolean or null");
                }
            }

            if (map.TryGetValue("interface", out val))
            {
                if (val is ByteString interf)
                {
                    if (!Enum.TryParse<Nep25Interface>(interf.GetString(), false, out var inferValue))
                        throw new FormatException("Incorrect NEP-25 interface");

                    Interface = inferValue;
                }
                else
                {
                    Interface = null;
                    if (val is not null) throw new FormatException("Interface must be ByteString or null");
                }
            }

            if (map.TryGetValue("key", out val))
            {
                if (val is ByteString key)
                {
                    if (!Enum.TryParse<ContractParameterType>(key.GetString(), false, out var keyValue))
                        throw new FormatException("Incorrect Parameter Type");

                    Key = keyValue;
                }
                else
                {
                    Key = null;
                    if (val is not null) throw new FormatException("Key must be ByteString or null");
                }
            }

            if (map.TryGetValue("value", out val))
            {
                if (val is Struct value)
                {
                    Value = new ExtendedType();
                    Value.FromStackItem(value);
                }
                else
                {
                    Value = null;
                    if (val is not null) throw new FormatException("Value must be Struct or null");
                }
            }

            if (map.TryGetValue("fields", out val))
            {
                if (val is VM.Types.Array fields)
                {
                    Fields = new ContractParameterDefinition[fields.Count];
                    for (var i = 0; i < fields.Count; i++)
                    {
                        var field = new ContractParameterDefinition();
                        field.FromStackItem((VM.Types.Array)fields[i]);
                        Fields[i] = field;
                    }
                }
                else
                {
                    Fields = null;
                    if (val is not null) throw new FormatException("Fields must be Array or null");
                }
            }
        }

        internal StackItem ToStackItem(IReferenceCounter referenceCounter)
        {
            var map = new Map(referenceCounter);
            return ToStackItem(referenceCounter, map);
        }

        internal Map ToStackItem(IReferenceCounter referenceCounter, Map map)
        {
            map["type"] = (byte)Type;

            if (NamedType != null) map["namedtype"] = NamedType;
            if (Length != null) map["length"] = Length;
            if (ForbidNull != null) map["forbidnull"] = ForbidNull;
            if (Interface != null) map["interface"] = Interface.ToString()!;
            if (Key != null) map["key"] = Key.ToString()!;
            if (Value != null) map["value"] = Value.ToStackItem(referenceCounter);
            if (Fields != null)
            {
                var fields = new VM.Types.Array(referenceCounter);
                foreach (var field in Fields)
                {
                    fields.Add(field.ToStackItem(referenceCounter));
                }
                map["fields"] = fields;
            }
            return map;
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter referenceCounter)
        {
            var item = new Map(referenceCounter);
            ToStackItem(referenceCounter, item);
            return item;
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
                if (!Enum.TryParse<ContractParameterType>(json["key"]!.GetString(), true, out var keyValue))
                    throw new FormatException("Invalid key value.");
                type.Key = keyValue;
            }
            if (json["value"] is JObject jValue)
            {
                type.Value = FromJson(jValue);
            }
            if (json["fields"] is JArray jFields)
            {
                type.Fields = new ContractParameterDefinition[jFields.Count];

                for (var i = 0; i < jFields.Count; i++)
                {
                    if (jFields[i] is not JObject jField)
                        throw new FormatException("Invalid Field entry");

                    type.Fields[i] = ContractParameterDefinition.FromJson(jField);
                }
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
            if (Value != null)
            {
                json["value"] = Value.ToJson();
            }
            if (Fields != null)
            {
                var jFields = new JArray();

                foreach (var field in Fields)
                {
                    jFields.Add(field.ToJson());
                }

                json["fields"] = jFields;
            }
            return json;
        }

        public override bool Equals(object? obj) => Equals(obj as ExtendedType);
        public override int GetHashCode() => HashCode.Combine(Type, NamedType, Length, ForbidNull, Interface, Key, Value, Fields?.Length ?? -1);

        public bool Equals(ExtendedType? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (Type != other.Type
                || NamedType != other.NamedType
                || Length != other.Length
                || ForbidNull != other.ForbidNull
                || Interface != other.Interface
                || Key != other.Key
                || !Equals(Value, other.Value))
                return false;

            if (Fields == null != (other.Fields == null)) return false;

            if (Fields != null)
            {
                if (Fields.Length != other.Fields!.Length) return false;

                for (var i = 0; i < Fields.Length; i++)
                {
                    if (!Equals(Fields[i], other.Fields[i])) return false;
                }
            }

            return true;
        }
    }
#nullable disable
}

