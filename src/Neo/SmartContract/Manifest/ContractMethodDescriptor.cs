// Copyright (C) 2015-2025 The Neo Project.
//
// ContractMethodDescriptor.cs file belongs to the neo project and is free
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
using System.Runtime.CompilerServices;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Represents a method in a smart contract ABI.
    /// </summary>
    public class ContractMethodDescriptor : ContractEventDescriptor, IEquatable<ContractMethodDescriptor>
    {
        /// <summary>
        /// Indicates the return type of the method. It can be any value of <see cref="ContractParameterType"/>.
        /// </summary>
        public ContractParameterType ReturnType { get; set; }

        /// <summary>
        /// NEP-25 extended return type
        /// </summary>
        public ExtendedType ExtendedReturnType { get; set; }

        /// <summary>
        /// The position of the method in the contract script.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Indicates whether the method is a safe method.
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by other contracts.
        /// </summary>
        public bool Safe { get; set; }

        public override bool HasNEP25
        {
            get
            {
                return base.HasNEP25 || ExtendedReturnType != null;
            }
        }

        public override void FromStackItem(StackItem stackItem)
        {
            base.FromStackItem(stackItem);
            var item = (Struct)stackItem;
            ReturnType = (ContractParameterType)(byte)item[2].GetInteger();
            Offset = (int)item[3].GetInteger();
            Safe = item[4].GetBoolean();

            if (item.Count >= 6)
            {
                ExtendedReturnType = new ExtendedType();
                ExtendedReturnType.FromStackItem(item[5]);
                ExtendedReturnType.ValidateForParameterOrReturn(ReturnType, null);
            }
            else
            {
                ExtendedReturnType = null;
            }
        }

        public override StackItem ToStackItem(IReferenceCounter? referenceCounter)
        {
            var item = (Struct)base.ToStackItem(referenceCounter);
            item.Add((byte)ReturnType);
            item.Add(Offset);
            item.Add(Safe);
            if (ExtendedReturnType != null)
            {
                item.Add(ExtendedReturnType.ToStackItem(referenceCounter));
            }
            return item;
        }

        /// <summary>
        /// Converts the method from a JSON object.
        /// </summary>
        /// <param name="json">The method represented by a JSON object.</param>
        /// <param name="knownNamedTypes">Set of named type identifiers declared in the manifest, if any.</param>
        /// <returns>The converted method.</returns>
        public new static ContractMethodDescriptor FromJson(JObject json, ISet<string> knownNamedTypes = null)
        {
            ContractMethodDescriptor descriptor = new()
            {
                Name = json["name"]!.GetString(),
                Parameters = ((JArray)json["parameters"]!).Select(u => ContractParameterDefinition.FromJson((JObject)u!, knownNamedTypes)).ToArray(),
                ReturnType = Enum.Parse<ContractParameterType>(json["returntype"]!.GetString()),
                Offset = json["offset"]!.GetInt32(),
                Safe = json["safe"]!.GetBoolean(),
                ExtendedReturnType = json["extendedreturntype"] != null ? ExtendedType.FromJson((JObject)json["extendedreturntype"]) : null
            };

            if (string.IsNullOrEmpty(descriptor.Name))
                throw new FormatException("Name in ContractMethodDescriptor is empty");

            _ = descriptor.Parameters.ToDictionary(p => p.Name);
            if (!Enum.IsDefined(typeof(ContractParameterType), descriptor.ReturnType))
                throw new FormatException($"ReturnType({descriptor.ReturnType}) in ContractMethodDescriptor is not valid");
            if (descriptor.Offset < 0)
                throw new FormatException($"Offset({descriptor.Offset}) in ContractMethodDescriptor is not valid");
            descriptor.ExtendedReturnType?.ValidateForParameterOrReturn(descriptor.ReturnType, knownNamedTypes);
            return descriptor;
        }

        /// <summary>
        /// Converts the method to a JSON object.
        /// </summary>
        /// <returns>The method represented by a JSON object.</returns>
        public override JObject ToJson()
        {
            var json = base.ToJson();
            json["returntype"] = ReturnType.ToString();
            json["offset"] = Offset;
            json["safe"] = Safe;
            if (ExtendedReturnType != null)
            {
                json["extendedreturntype"] = ExtendedReturnType.ToJson();
            }
            return json;
        }

        public bool Equals(ContractMethodDescriptor? other)
        {
            if (ReferenceEquals(this, other)) return true;

            return
                base.Equals(other) && // Already check null
                ReturnType == other.ReturnType
                && Offset == other.Offset
                && Safe == other.Safe
                && Equals(ExtendedReturnType, other.ExtendedReturnType);
        }

        public override bool Equals(object? other)
        {
            if (other is not ContractMethodDescriptor ev)
                return false;

            return Equals(ev);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ReturnType, Offset, Safe, ExtendedReturnType?.GetHashCode() ?? -1, base.GetHashCode());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ContractMethodDescriptor left, ContractMethodDescriptor right)
        {
            if (left is null || right is null)
                return Equals(left, right);

            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ContractMethodDescriptor left, ContractMethodDescriptor right)
        {
            if (left is null || right is null)
                return !Equals(left, right);

            return !left.Equals(right);
        }
    }
}
