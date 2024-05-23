// Copyright (C) 2015-2024 The Neo Project.
//
// ContractParameterDefinition.cs file belongs to the neo project and is free
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
    /// <summary>
    /// Represents a parameter of an event or method in ABI.
    /// </summary>
    public class ContractParameterDefinition : IInteroperable
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the parameter. It can be any value of <see cref="ContractParameterType"/> except <see cref="ContractParameterType.Void"/>.
        /// </summary>
        public ContractParameterType Type { get; set; }

        /// <summary>
        /// NEP-25 extended type
        /// </summary>
        public ExtendedType? ExtendedType { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            var @struct = (Struct)stackItem;
            Name = @struct[0].GetString();
            Type = (ContractParameterType)(byte)@struct[1].GetInteger();

            if (@struct.Count >= 3)
            {
                ExtendedType = new ExtendedType();
                ExtendedType.FromStackItem((VM.Types.Array)@struct[5], 0);
            }
            else
            {
                ExtendedType = null;
            }
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            var @struct = new Struct(referenceCounter) { Name, (byte)Type };

            if (ExtendedType != null)
            {
                var structExtended = new Struct(referenceCounter);
                @struct.Add(ExtendedType.ToStackItem(referenceCounter, structExtended));
            }

            return @struct;
        }

        /// <summary>
        /// Converts the parameter from a JSON object.
        /// </summary>
        /// <param name="json">The parameter represented by a JSON object.</param>
        /// <returns>The converted parameter.</returns>
        public static ContractParameterDefinition FromJson(JObject json)
        {
            var parameter = new ContractParameterDefinition()
            {
                Name = json["name"].GetString(),
                Type = Enum.Parse<ContractParameterType>(json["type"].GetString()),
                ExtendedType = json["extendedtype"] != null ? ExtendedType.FromJson((JObject)json["extendedtype"]) : null,
            };
            if (string.IsNullOrEmpty(parameter.Name))
                throw new FormatException();
            if (!Enum.IsDefined(typeof(ContractParameterType), parameter.Type) || parameter.Type == ContractParameterType.Void)
                throw new FormatException();
            return parameter;
        }

        /// <summary>
        /// Converts the parameter to a JSON object.
        /// </summary>
        /// <returns>The parameter represented by a JSON object.</returns>
        public JObject ToJson()
        {
            var json = new JObject();
            json["name"] = Name;
            json["type"] = Type.ToString();
            if (ExtendedType != null)
            {
                json["extendedtype"] = ExtendedType.ToString();
            }
            return json;
        }
    }
}
