// Copyright (C) 2015-2024 The Neo Project.
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
    public class ExtendedType : IInteroperable
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
        public string NamedType { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            FromStackItem((VM.Types.Array)stackItem, 0);
        }

        internal void FromStackItem(VM.Types.Array @struct, int startIndex)
        {
            Type = (ContractParameterType)(byte)@struct[startIndex].GetInteger();
            NamedType = @struct[startIndex + 1].GetString();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = new Struct(referenceCounter);
            ToStackItem(referenceCounter, @struct);
            return @struct;
        }

        internal StackItem ToStackItem(ReferenceCounter referenceCounter, Struct @struct)
        {
            @struct.Add((byte)Type);
            @struct.Add(NamedType);
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
                Type = Enum.Parse<ContractParameterType>(json["type"].GetString()),
                NamedType = json["namedtype"].GetString(),
            };
            if (!Enum.IsDefined(typeof(ContractParameterType), type.Type)) throw new FormatException();
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
            return json;
        }
    }
}
