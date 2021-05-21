using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Represents a trigger in a smart contract ABI.
    /// </summary>
    public class ContractTriggerDescriptor : IInteroperable
    {
        /// <summary>
        /// Block Height of the Trigger
        /// </summary>
        public uint Height { get; set; }

        /// <summary>
        /// The name of the trigger or method.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The parameters of the trigger or method.
        /// </summary>
        public ContractParameterDefinition[] Parameters { get; set; }

        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Height = (uint)@struct[0].GetInteger();
            Name = @struct[1].GetString();
            Parameters = ((Array)@struct[2]).Select(p => p.ToInteroperable<ContractParameterDefinition>()).ToArray();
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
        /// Converts the trigger from a JSON object.
        /// </summary>
        /// <param name="json">The trigger represented by a JSON object.</param>
        /// <returns>The converted event.</returns>
        public static ContractTriggerDescriptor FromJson(JObject json)
        {
            ContractTriggerDescriptor descriptor = new()
            {
                Height = (uint)json["height"].GetInt32(),
                Name = json["name"].GetString(),
                Parameters = ((JArray)json["parameters"]).Select(u => ContractParameterDefinition.FromJson(u)).ToArray(),
            };
            if (string.IsNullOrEmpty(descriptor.Name)) throw new FormatException();
            _ = descriptor.Parameters.ToDictionary(p => p.Name);
            return descriptor;
        }

        /// <summary>
        /// Converts the trigger to a JSON object.
        /// </summary>
        /// <returns>The trigger represented by a JSON object.</returns>
        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["height"] = Height;
            json["name"] = Name;
            json["parameters"] = new JArray(Parameters.Select(u => u.ToJson()).ToArray());
            return json;
        }
    }
}
