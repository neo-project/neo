// Copyright (C) 2015-2025 The Neo Project.
//
// ContractAbi.cs file belongs to the neo project and is free
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
        private IReadOnlyDictionary<(string, int), ContractMethodDescriptor>? _methodDictionary;
        private const int STATE_UNCHECK = 0;
        private const int STATE_CHECKING = 1;
        private const int STATE_CHECK = 2;

        /// <summary>
        /// Gets the methods in the ABI.
        /// </summary>
        public required ContractMethodDescriptor[] Methods { get; set; }

        /// <summary>
        /// Gets the events in the ABI.
        /// </summary>
        public required ContractEventDescriptor[] Events { get; set; }

        /// <summary>
        /// An object with each member having a name (a string consisting of one or more identifiers joined by dots) and a value of ExtendedType object.
        /// </summary>
        public Dictionary<string, ExtendedType>? NamedTypes { get; set; }

        public bool HasNEP25
        {
            get
            {
                return NamedTypes != null || Events.Any(x => x.HasNEP25) || Methods.Any(x => x.HasNEP25);
            }
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            var data = (Struct)stackItem;
            Methods = [.. ((Array)data[0]).Select(p => p.ToInteroperable<ContractMethodDescriptor>())];
            Events = [.. ((Array)data[1]).Select(p => p.ToInteroperable<ContractEventDescriptor>())];

            if (data.Count >= 3 && !data[2].IsNull)
                NamedTypes = ((Map)data[2]).ToDictionary(p => p.Key.GetString()!, p => p.Value.ToInteroperable<ExtendedType>());
            else
                NamedTypes = null;

            ValidateExtendedTypes();
        }

        public StackItem ToStackItem(IReferenceCounter? referenceCounter)
        {
            var ret = new Struct(referenceCounter)
            {
                new Array(referenceCounter, Methods.Select(p => p.ToStackItem(referenceCounter))),
                new Array(referenceCounter, Events.Select(p => p.ToStackItem(referenceCounter)))
            };

            if (NamedTypes != null)
            {
                var map = new Map(referenceCounter);

                foreach (var nt in NamedTypes)
                {
                    map[nt.Key] = nt.Value.ToStackItem(referenceCounter);
                }

                ret.Add(map);
            }

            return ret;
        }

        /// <summary>
        /// Converts the ABI from a JSON object.
        /// </summary>
        /// <param name="json">The ABI represented by a JSON object.</param>
        /// <returns>The converted ABI.</returns>
        public static ContractAbi FromJson(JObject json)
        {
            Dictionary<string, ExtendedType>? namedTypes = null;
            var knownNamedTypes = new HashSet<string>(StringComparer.Ordinal);
            if (json!["namedtypes"] is JObject namedTypesJson)
            {
                foreach (var key in namedTypesJson.Properties.Keys)
                {
                    knownNamedTypes.Add(key);
                }

                namedTypes = new(namedTypesJson.Properties.Count, StringComparer.Ordinal);
                foreach (var (name, token) in namedTypesJson.Properties)
                {
                    if (token is not JObject valueObject)
                        throw new FormatException("Named type definition must be a JSON object.");
                    namedTypes[name] = ExtendedType.FromJson(valueObject);
                }
            }

            ContractAbi abi = new()
            {
                Methods = ((JArray?)json["methods"])?.Select(u => ContractMethodDescriptor.FromJson((JObject)u!)).ToArray() ?? [],
                Events = ((JArray?)json["events"])?.Select(u => ContractEventDescriptor.FromJson((JObject)u!)).ToArray() ?? [],
                NamedTypes = namedTypes
            };
            if (abi.Methods.Length == 0) throw new FormatException("Methods in ContractAbi is empty");

            abi.ValidateExtendedTypes();
            return abi;
        }

        private static bool HasCircularReference(string name, IReadOnlyDictionary<string, ExtendedType> namedTypes, Dictionary<string, int> states)
        {
            if (!states.TryGetValue(name, out var state))
                state = STATE_UNCHECK;

            if (state == STATE_CHECKING) return true;
            if (state == STATE_CHECK) return false;

            states[name] = STATE_CHECKING;

            var next = namedTypes[name].NamedType;
            if (next is not null && namedTypes.ContainsKey(next))
            {
                if (HasCircularReference(next, namedTypes, states))
                    return true;
            }

            states[name] = STATE_CHECK;
            return false;
        }

        internal void ValidateExtendedTypes()
        {
            ISet<string> knownNamedTypes = NamedTypes != null
                ? new HashSet<string>(NamedTypes.Keys, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            if (NamedTypes != null)
            {
                var states = new Dictionary<string, int>(NamedTypes.Count, StringComparer.Ordinal);
                foreach (var (name, type) in NamedTypes)
                {
                    ExtendedType.EnsureValidNamedTypeIdentifier(name);
                    if (HasCircularReference(name, NamedTypes, states))
                    {
                        throw new FormatException($"Circular reference in namedtypes starting at '{name}'");
                    }

                    type.ValidateForNamedTypeDefinition(name, knownNamedTypes);
                }
            }

            foreach (var method in Methods)
            {
                foreach (var parameter in method.Parameters)
                {
                    parameter.ExtendedType?.ValidateForParameterOrReturn(parameter.Type, knownNamedTypes);
                }

                method.ExtendedReturnType?.ValidateForParameterOrReturn(method.ReturnType, knownNamedTypes);
            }

            foreach (var ev in Events)
            {
                foreach (var parameter in ev.Parameters)
                {
                    parameter.ExtendedType?.ValidateForParameterOrReturn(parameter.Type, knownNamedTypes);
                }
            }
        }

        /// <summary>
        /// Gets the method with the specified name.
        /// </summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="pcount">
        /// The number of parameters of the method.
        /// It can be set to -1 to search for the method with the specified name and any number of parameters.
        /// </param>
        /// <returns>
        /// The method that matches the specified name and number of parameters.
        /// If <paramref name="pcount"/> is set to -1, the first method with the specified name will be returned.
        /// </returns>
        public ContractMethodDescriptor? GetMethod(string name, int pcount)
        {
            if (pcount < -1 || pcount > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(pcount), $"`pcount` must be between [-1, {ushort.MaxValue}]");
            if (pcount >= 0)
            {
                _methodDictionary ??= Methods.ToDictionary(p => (p.Name, p.Parameters.Length));
                if (_methodDictionary.TryGetValue((name, pcount), out var method))
                    return method;

                return null;
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
            var ret = new JObject()
            {
                ["methods"] = new JArray([.. Methods.Select(u => u.ToJson())]),
                ["events"] = new JArray([.. Events.Select(u => u.ToJson())])
            };

            if (NamedTypes != null)
            {
                ret["namedtypes"] = new JObject(NamedTypes.ToDictionary(u => u.Key, u => (JToken?)u.Value.ToJson()));
            }

            return ret;
        }
    }
}
