// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Represents an event in a smart contract ABI.
    /// </summary>
    public class ContractEventDescriptor : IInteroperable
    {
        /// <summary>
        /// The name of the event or method.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The parameters of the event or method.
        /// </summary>
        public ContractParameterDefinition[] Parameters { get; set; }

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Name = @struct[0].GetString();
            Parameters = ((Array)@struct[1]).Select(p => p.ToInteroperable<ContractParameterDefinition>()).ToArray();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                Name,
                new Array(referenceCounter, Parameters.Select(p => p.ToStackItem(referenceCounter)))
            };
        }

        /// <summary>
        /// Converts the event from a JSON object.
        /// </summary>
        /// <param name="json">The event represented by a JSON object.</param>
        /// <returns>The converted event.</returns>
        public static ContractEventDescriptor FromJson(JObject json)
        {
            ContractEventDescriptor descriptor = new()
            {
                Name = json["name"].GetString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.FromJson(u)).ToArray(),
            };
            if (string.IsNullOrEmpty(descriptor.Name)) throw new FormatException();
            _ = descriptor.Parameters.ToDictionary(p => p.Name);
            return descriptor;
        }

        /// <summary>
        /// Converts the event to a JSON object.
        /// </summary>
        /// <returns>The event represented by a JSON object.</returns>
        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["name"] = Name;
            json["parameters"] = new JArray(Parameters.Select(u => u.ToJson()).ToArray());
            return json;
        }
    }
}
