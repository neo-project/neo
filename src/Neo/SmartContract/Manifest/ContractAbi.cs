// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Represents the ABI of a smart contract.
    /// </summary>
    /// <remarks>For more details, see NEP-14.</remarks>
    public class ContractAbi : IInteroperable
    {
        private IReadOnlyDictionary<(string, int), ContractMethodDescriptor> methodDictionary;

        /// <summary>
        /// Gets the methods in the ABI.
        /// </summary>
        public ContractMethodDescriptor[] Methods { get; set; }

        /// <summary>
        /// Gets the events in the ABI.
        /// </summary>
        public ContractEventDescriptor[] Events { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Methods = ((Array)@struct[0]).Select(p => p.ToInteroperable<ContractMethodDescriptor>()).ToArray();
            Events = ((Array)@struct[1]).Select(p => p.ToInteroperable<ContractEventDescriptor>()).ToArray();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                new Array(referenceCounter, Methods.Select(p => p.ToStackItem(referenceCounter))),
                new Array(referenceCounter, Events.Select(p => p.ToStackItem(referenceCounter))),
            };
        }

        /// <summary>
        /// Converts the ABI from a JSON object.
        /// </summary>
        /// <param name="json">The ABI represented by a JSON object.</param>
        /// <returns>The converted ABI.</returns>
        public static ContractAbi FromJson(JObject json)
        {
            ContractAbi abi = new()
            {
                Methods = ((JArray)json["methods"]).Select(u => ContractMethodDescriptor.FromJson(u.GetObject())).ToArray(),
                Events = ((JArray)json["events"]).Select(u => ContractEventDescriptor.FromJson(u.GetObject())).ToArray()
            };
            if (abi.Methods.Length == 0) throw new FormatException();
            return abi;
        }

        /// <summary>
        /// Gets the method with the specified name.
        /// </summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="pcount">The number of parameters of the method. It can be set to -1 to search for the method with the specified name and any number of parameters.</param>
        /// <returns>The method that matches the specified name and number of parameters. If <paramref name="pcount"/> is set to -1, the first method with the specified name will be returned.</returns>
        public ContractMethodDescriptor GetMethod(string name, int pcount)
        {
            if (pcount < -1 || pcount > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(pcount));
            if (pcount >= 0)
            {
                methodDictionary ??= Methods.ToDictionary(p => (p.Name, p.Parameters.Length));
                methodDictionary.TryGetValue((name, pcount), out var method);
                return method;
            }
            else
            {
                return Methods.FirstOrDefault(p => p.Name == name);
            }
        }

        /// <summary>
        /// Converts the ABI to a JSON object.
        /// </summary>
        /// <returns>The ABI represented by a JSON object.</returns>
        public JObject ToJson()
        {
            var json = new JObject();
            json["methods"] = new JArray(Methods.Select(u => u.ToJson()).ToArray());
            json["events"] = new JArray(Events.Select(u => u.ToJson()).ToArray());
            return json;
        }
    }
}
